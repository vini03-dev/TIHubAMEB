# Changelog — TIHub AMEB

Todas as mudanças relevantes entre versões são documentadas aqui.
Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).

---

## [1.1.0] — 2026-07-01

### Novas funcionalidades

#### Aba Programas (nova)
- **Inventário de uma máquina:** lista todos os programas instalados de
  qualquer máquina (local ou remota), lendo as três chaves do registro do
  Windows (HKLM 64-bit, HKLM 32-bit via WOW6432Node e HKCU). Exibe nome,
  versão, fabricante e data de instalação. Filtro em tempo real por nome ou
  fabricante.
- **Busca em massa:** localiza um programa em todas as máquinas online ao
  mesmo tempo (máx. 20 simultâneas via semáforo), com barra de progresso e
  timeout por máquina de 25 segundos. Útil para mapear qual versão de um
  software está instalada no parque — usado especialmente para rastrear o
  Kaspersky durante a migração para o Falcon.
- Os dois modos de uso (lista e busca) são alternados por botões no topo da
  aba, com colunas ajustadas automaticamente para cada contexto.

#### Gerenciador de Inicialização (nova seção na aba Otimização)
- Lista todos os programas configurados para iniciar com o Windows, lendo as
  chaves `Run` de HKLM e HKCU, local ou remotamente.
- **Desativação reversível:** em vez de apagar o item do registro, move-o
  para uma chave de backup (`Run-TIHubDisabled`). O item pode ser reativado a
  qualquer momento com um clique, sem risco de perda.
- Itens desativados aparecem em cor diferente (cinza) para distinção visual.
- Funciona na máquina local e em qualquer máquina remota do domínio.

#### Distribuição de arquivos (aba Máquinas)
- Novo botão **Distribuir** permite copiar um ou mais arquivos para o Desktop
  Público de várias máquinas ao mesmo tempo, via compartilhamento `C$`.
- Detecta automaticamente o idioma da pasta Desktop (português: "Área de
  Trabalho" / inglês: "Desktop").
- Cópias paralelas com semáforo (máx. 15 simultâneas), barra de progresso e
  relatório final de sucesso/falha por máquina.

#### Auto-refresh de status (aba Máquinas)
- Timer automático de 3 minutos que reatualiza o status online/offline de
  todas as máquinas sem intervenção manual.
- Checkbox na interface para ligar e desligar o auto-refresh.
- Protegido contra execução simultânea: se uma verificação já estiver rodando
  ou o sistema estiver ocupado com outra operação pesada (limpeza, busca em
  massa, etc.), o timer pula aquela rodada.

#### Filtro por status online/offline (aba Máquinas)
- Botão que alterna entre três estados: **Todos → Online → Offline → Todos**.
- Combinado com o filtro de texto existente: os dois filtros funcionam juntos
  ao mesmo tempo.

#### Cópia de dados das máquinas (aba Máquinas)
- **Ctrl+C** copia a linha inteira da máquina selecionada, com colunas
  separadas por TAB — pronto para colar diretamente no Excel.
- **Clique com botão direito** abre um menu contextual com opção de copiar
  o valor da célula específica clicada ou a linha inteira.

#### Redesign visual (UIHelper)
- Paleta de cores refinada com mais profundidade entre as camadas
  (fundo → painel → card), aumentando o contraste e a legibilidade.
- Raios de canto padronizados como constantes (`RaioCard`, `RaioPainel`,
  `RaioBotao`, `RaioInput`, `RaioBarra`) para harmonia entre os elementos.
- Novas fontes estáticas: `FonteCardNome` (10,5pt bold) e `FontePequenaBold`
  (8,5pt bold) para os cards de impressora.
- Novos métodos de estilização de controles Guna 2 adicionados ao helper.

---

### Correções de bugs

- **Timeout do PSExec nunca disparava:** a verificação de timeout em
  `PsExecService.RodarProcessoAsync` comparava o resultado do `Task.WhenAny`
  com um novo objeto `Task.Delay()` criado na hora — que nunca seria igual ao
  que estava dentro do `WhenAny`. O `kill` do processo travado era código
  inalcançável. Corrigido guardando a referência do `Task.Delay` em variável
  antes do `WhenAny` e comparando contra ela. A partir desta versão, processos
  PSExec que travam por mais de 120 segundos são encerrados corretamente.

- **Botões não bloqueavam durante operações do sistema:** `BloqueiarBotoes`
  tinha a linha `btnLimpezaLeve` repetida duas vezes (onde deveria estar
  `btnLimpezaMedia`), e `BloqueiarBotoesSistema` repetia `btnCheckHealth`
  (onde deveria estar `btnScanHealth`). Na prática, esses botões não eram
  desabilitados durante operações longas. Ambos corrigidos.

---

### Melhorias de performance

- **WMI com controle de concorrência (aba Máquinas):** a verificação de
  status disparava uma conexão WMI por máquina online sem nenhum limite —
  chegando a ~280 conexões simultâneas com todo o parque online. Agora há um
  semáforo próprio para WMI (máx. 10 simultâneas), separado do semáforo de
  ping. O método só retorna quando todos os dados de usuário e modelo já
  chegaram, eliminando também a condição de corrida que causava a coluna
  "Usuário" aparecer vazia e preencher de forma imprevisível.

- **`PerformanceCounter` reutilizado no Dashboard:** o contador de CPU era
  criado e descartado a cada tick do timer de 5 segundos — um objeto custoso
  de instanciar por precisar abrir handles no kernel do Windows. Agora vive
  como campo da classe e é criado uma única vez. O `Thread.Sleep(300)` de
  warm-up foi removido junto: com o contador persistindo entre ticks, o
  intervalo de 5 segundos do timer já garante leitura precisa.

- **Script PowerShell montado uma vez na busca em massa:** em
  `ProgramasService.BuscarProgramaEmMassaAsync`, o script era construído
  dentro do `Select` — ou seja, reconstruído uma vez por máquina (~280 vezes
  para o mesmo termo). Movido para fora do loop.

- **`JsonSerializerOptions` como `static readonly`:** em
  `ImpressoraService.CarregarCadastroAsync`, a instância era criada a cada
  chamada, descartando o cache interno do serializador. Virou campo estático
  compartilhado entre chamadas.

- **Lock no log para evitar colisão de arquivo:** `LogService.SalvarEmArquivo`
  usava `File.AppendAllText` sem sincronização. Durante verificações em massa,
  várias threads tentavam abrir o mesmo arquivo de log ao mesmo tempo,
  causando `IOException` descartada silenciosamente pelo `catch {}` — ou seja,
  entradas de log sumiam. Adicionado `lock` para serializar os escritas.

- **Fontes dos cards de impressora sem vazamento de GDI:** `CriarCardImpressora`
  criava dois objetos `Font` inline por card. Com 41 impressoras, cada
  atualização da lista gerava 82 handles GDI que nunca eram descartados.
  As fontes viraram `static readonly` em `UIHelper` e são compartilhadas por
  todos os cards.

- **Coleta de resultados sem lock em busca em massa:** em
  `ProgramasService.BuscarProgramaEmMassaAsync`, o `lock + AddRange` na lista
  de resultados foi substituído por `ConcurrentBag`, eliminando o ponto de
  contenção entre threads.

---

### Manutenção e limpeza de código

- **`MontarModelo` unificado:** o método que formata o nome do fabricante e
  modelo de uma máquina estava copiado palavra por palavra em
  `MonitoramentoService` e `MaquinasService`. Movido para
  `MaquinaRede.MontarModelo` como método público estático; ambos os services
  agora chamam a mesma implementação.

- **`DetectarSetor` unificado:** `MaquinaInfo.DetectarSetor` e
  `MaquinaRede.DetectarSetor` tinham lógicas ligeiramente diferentes (3 letras
  exatas vs. 2–5 letras). `MaquinaInfo.DetectarSetor` agora delega para
  `MaquinaRede.DetectarSetor`, garantindo comportamento idêntico nos dois
  contextos onde o setor é detectado.

- **Log de diagnóstico removido da produção:** o timer de auto-refresh
  registrava "Timer disparou às HH:mm:ss" a cada 3 minutos em produção.
  Era um log temporário de diagnóstico que havia ficado no código.

- **`CancellationTokenSource` inútil removido:** `PsExecService.RodarProcessoAsync`
  criava um `CancellationTokenSource` com timeout de 120s que nunca era
  conectado a nenhum método — o token não era passado para lugar algum.
  Removido para não gerar confusão.

- **`Task.Delay(100)` sem justificativa removido:** `PsExecService.ExecutarVariosAsync`
  aguardava 100ms entre cada comando sequencial sem nenhuma razão documentada
  ou técnica. Removido.

- **`CLAUDE.md` adicionado ao repositório:** documentação técnica do projeto
  para orientar futuras manutenções — arquitetura, regras aprendidas na
  prática (PSExec, WMI, detecção de IP, versão do Windows), decisões de
  escopo deliberadas e contexto do ambiente hospitalar.

---

## [1.0.0] — 2026-06-22

Lançamento inicial. Funcionalidades incluídas:

- **Dashboard** com métricas em tempo real de CPU, RAM e disco (local e
  remoto via WMI), com atualização automática a cada 5 segundos.
- **Limpeza** com três perfis prontos (Leve, Médio, Avançado) e modo
  personalizado; executa local ou remotamente via PSExec.
- **Otimização** com perfis de performance (Escritório, Hospital,
  Ultra Performance); ajusta registro no perfil do usuário logado via SID.
- **Rede** com ping, flush DNS, reiniciar Explorer, desligar/reiniciar
  máquina, RDP, Assistência Remota, console PowerShell remoto e acesso ao
  compartilhamento C$.
- **Máquinas** com busca no Active Directory (filtro de servidores pelo
  atributo `operatingSystem`), verificação de status com ping em paralelo
  (máx. 20 simultâneos) e coluna de usuário conectado via WMI.
- **Usuários** com listagem de perfis locais, tamanho de pasta e exclusão
  remota protegida (usuário atual e contas de sistema são bloqueados).
- **Sistema** com DISM CheckHealth, ScanHealth, RestoreHealth e SFC /scannow,
  localmente ou em máquina remota.
- **Impressoras** com monitoramento de nível de toner via SNMP (Printer-MIB),
  fallback de ping, cards visuais com alertas, acesso ao painel web e limpeza
  de fila de impressão.
- **Logs** com exibição colorida em tempo real por tipo de operação e
  salvamento em arquivo diário.
