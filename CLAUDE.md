# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Atenção:** Este arquivo tem duas partes. A primeira (estrutural) foi gerada pelo `/init` lendo o código-fonte. A segunda (histórico e decisões) foi escrita manualmente e cobre o que não dá para descobrir lendo o código. **Leia as duas antes de propor mudanças.**

---

## Projeto

**TIHub AMEB** — aplicativo desktop WinForms (.NET 10, C#) para suporte de TI em ambiente hospitalar. Um único `TIHubAMEB.csproj`, sem arquivo de solução separado. Requer elevação de Administrador (forçado via `app.manifest`).

- **Namespace:** `TIHubAMEB`
- **Tema visual:** dark azulado flat (sem sombras), via Guna 2 UI + `Helpers/UIHelper.cs`
- **Abas:** Dashboard, Limpeza, Otimização, Rede, Máquinas, Usuários, Sistema, Impressoras, Programas, Logs

---

## Build & Run

```bash
# Build
dotnet build

# Rodar (deve ser como Administrador por causa do app.manifest)
dotnet run

# Publicar como executável único auto-contido (recomendado para distribuição)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
# Saída: bin/Release/net10.0-windows/publish/win-x64/
```

Não existe projeto de testes ainda.

**Se o build falhar com "PsExec64.exe bloqueado":** matar processos `PsExec64.exe` órfãos antes de compilar:
```
taskkill /F /IM PsExec64.exe /T
```

---

## Arquitetura

Separação em três camadas: **MainForm (UI) → Services (lógica) → Models (dados)**

- `MainForm` orquestra chamadas de serviço e atualiza a UI. Nenhuma lógica de negócio vive aqui.
- Todos os serviços são instanciados manualmente em `MainForm()` em ordem de dependência. `LogService` é criado primeiro; todos os outros recebem ele como argumento de construtor.
- `PsExecService` é a segunda dependência central — encapsula execução local e remota, injetado em `LimpezaService`, `OtimizacaoService`, `InicializacaoService`, `RedeService`, `UsuarioService`, `SistemaService` e `ProgramasService`.

**Toda operação que toca rede, WMI, AD, PSExec ou SNMP deve ser `async`.** Usar `CancellationTokenSource _cts` (definido em `MainForm`) para cancelar operações longas. A flag `_sistemaOcupado` em `MainForm` impede operações destrutivas concorrentes.

### Serviços — notas críticas

| Serviço | Mecanismo | Detalhe crítico |
|---|---|---|
| `MonitoramentoService` | `PerformanceCounter` (local), WMI (remoto) | Retorna resultado em cache imediatamente, dispara coleta fresca em background |
| `MaquinasService` | `DirectoryServices` (AD) | Filtra servidores pelo atributo `operatingSystem` do AD, não pelo hostname. Cache de 5 min. Ping usa `SemaphoreSlim(20)`. Coluna "Usuário" preenchida via WMI após ping. |
| `OtimizacaoService` | PowerShell + PSExec | Escreve em `HKEY_USERS\{SID}` (não `HKCU`), porque PSExec roda como SYSTEM |
| `ImpressoraService` | SNMP (`SnmpSharpNet`) com fallback de ping | Ordenado por estado de alerta via `ImpressoraInfo.TemAlerta` / `TonerCritico` |
| `PsExecService` | `Process` + `PsExec64.exe` | `CreateNoWindow = true`. Timeout 120s, 2 auto-retentativas. Path resolvido de `AppDomain.CurrentDomain.BaseDirectory/Tools/` |

### UI — convenções

Todos os controles Guna 2 (`Guna.UI2.WinForms`) são estilizados via `UIHelper` (`Helpers/UIHelper.cs`). Todas as cores, fontes, raios de canto e constantes de sombra estão definidos lá — **não hardcodar valores visuais fora do `UIHelper`**.

Timers ativos:
- `_timerMonitor` (5s) — atualização de métricas do Dashboard
- `_timerAutoRefresh` (3 min) — atualização de status online/offline das máquinas; protegido pelas flags `_verificandoStatus` e `_sistemaOcupado`

---

## Assets externos (não versionados)

| Asset | Local | Fonte |
|---|---|---|
| `PsExec64.exe` | `Tools/PsExec64.exe` | [Sysinternals](https://learn.microsoft.com/sysinternals/downloads/pstools) |
| `impressoras.json` | `Data/impressoras.json` | Copiar de `Data/impressoras.exemplo.json` e preencher com IPs/nomes reais |

Ambos estão no `.gitignore`. O app não inicia corretamente sem `impressoras.json` (pode ser array vazio `[]`).

---

## Pacotes principais

| Pacote | Finalidade |
|---|---|
| `Guna.UI2.WinForms` | Controles WinForms estilizados (botões, painéis, barras de progresso) |
| `SnmpSharpNet` | SNMP v1/v2c para níveis de toner das impressoras |
| `System.Management` | Queries WMI (CPU, RAM, usuário logado, métricas remotas) |
| `System.DirectoryServices` + `.AccountManagement` | Busca de máquinas/usuários no Active Directory |

**Warnings de pacote que são INTENCIONAIS — não remover:**
- `NU1510` — `System.DirectoryServices`: o NuGet acha desnecessário, mas o app **precisa** dele para o AD.
- `NU1701` — `SnmpSharpNet`: pacote .NET Framework rodando em modo de compatibilidade. Funciona normal.

---

## Regras técnicas críticas (aprendidas na prática — não repetir os erros)

### PSExec + PowerShell — a regra mais importante

- Scripts PowerShell executados via `PsExecService.ExecutarPowerShellAsync(script, maquina)` **DEVEM ser de linha única** (comandos separados por `;`). Scripts multilinha **quebram silenciosamente** ao passar por `cmd /c powershell -Command "..."` — o comando retorna código 0 mas saída vazia. Sintoma: "0 itens lidos" mesmo com dados presentes.
- **Nunca usar `|` como separador de dados** na saída dos scripts. O `cmd.exe` interpreta `|` como pipe do shell e corta a saída. Usar **`~~` (dois tils)** como separador em qualquer saída estruturada (ex: `ORIGEM~~ESTADO~~NOME~~COMANDO`).
- Para validar um script novo: testar a versão de linha única direto no `cmd` com `cmd /c powershell.exe -NonInteractive -ExecutionPolicy Bypass -Command "..."`. Testar no PowerShell ISE pode passar e depois falhar no PSExec — o teste deve ser pela mesma corrente (cmd → powershell).
- Formato do comando PSExec: `psexec \\maquina -h -s -accepteula cmd /c "comando"`. Roda como **SYSTEM** (`-s`), elevado (`-h`), oculto (sem janela).
- Como roda como SYSTEM, `HKCU` aponta para o perfil do SYSTEM, não do usuário logado. Para ajustes no perfil do usuário real, detectar o **SID** via `explorer.exe` owner por WMI e usar `Registry::HKEY_USERS\{SID}\...`. Ver `OtimizacaoService.cs` para o padrão implementado.

### Detecção de IP local

Não usar `Dns.GetHostAddresses(Dns.GetHostName())` — em máquinas com Hyper-V/VirtualBox retorna IPs de adaptadores virtuais. O critério correto: iterar `NetworkInterface.GetAllNetworkInterfaces()` e pegar o IPv4 do adaptador que tem **Gateway Padrão válido** (não `0.0.0.0`). Não filtrar por nome do adaptador.

### Detecção de versão do Windows

Não confiar em `RuntimeInformation.OSDescription` nem em `so.Contains("11")` — no Windows 11, essas APIs ainda reportam "10.0.22000". Usar `Environment.OSVersion.Version.Build`: `>= 22000` = Win 11, `>= 10240` = Win 10.

### Busca de programas em massa

- Usar timeout por máquina (~25s) com `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter`. Se a máquina não responde, desiste e segue. Ver `ProgramasService.BuscarProgramaEmMassaAsync`.
- O script já filtra na própria máquina (via `Where-Object`) — cada máquina retorna só o que casou, mantendo a operação leve com ~280 máquinas online.

### WMI — boas práticas

- Buscar múltiplos dados numa só conexão WMI com uma query que traz tudo, em vez de abrir conexões separadas. Service Tag vem de `Win32_BIOS.SerialNumber`.
- O método `MontarModelo(fabricante, modelo)` existe **duplicado** em `MonitoramentoService.cs` e `MaquinasService.cs`. Se mudar a lógica, mudar nos dois.

### ListView — ordem das colunas

A ordem dos `item.SubItems.Add(...)` no `MainForm.cs` **deve bater exatamente** com a ordem das `Columns.Add(...)` no Designer. Ao adicionar/remover coluna, ajustar os dois lugares juntos.

### Distribuição de arquivos

Copia para o Desktop Público via `C$`. Detecta tanto "Área de Trabalho" (PT) quanto "Desktop" (EN). **Pegadinha de threading:** capturar `LogService log = _log` em variável local antes do `Task.Run`, senão dá NullReference no lambda.

---

## Decisões de escopo deliberadas (o que NÃO fazer e por quê)

- **NÃO implementar remoção do Kaspersky pelo TIHub.** O hospital usa Kaspersky Security Center (console central em AME-CONF-13). O Security Center pode reinstalar automaticamente o que for removido por fora; há várias versões (11.11, 11.13, 11.17, 12.1) com comportamentos distintos; a auto-proteção por senha resiste à desinstalação remota mesmo como SYSTEM. A remoção deve ser feita **pelo próprio Security Center**. O TIHub apenas **mapeia** quais máquinas têm Kaspersky e qual versão (já implementado na aba Programas).
- **Desinstalação remota genérica é só para programas comuns** (não-antivírus gerenciado). Ver backlog.
- **Responsividade total (Anchor global) foi revertida.** Abas de card (Dashboard, Limpeza, Otimização) têm posição fixa — Anchor global bagunçou o layout. Não reintroduzir sem refazer com cálculo proporcional. O método `ConfigurarResponsividade()` foi removido.
- **Limpeza de RAM, pagefile, desativar serviços do Windows, registry cleaners:** descartados. Risco alto para ganho baixo em ambiente hospitalar.
- **Alertas por e-mail:** adiados. Exige sistema rodando continuamente (serviço em background). Se for implementar: senha SMTP **nunca** no código (repo é público) — ler de arquivo de config no `.gitignore`.

---

## Backlog — funcionalidades planejadas mas não implementadas

- **Desinstalação remota de programas** (aba Programas): só para programas comuns (não-antivírus). Usar `UninstallString` / `QuietUninstallString` do registro. Detectar se pede senha e mostrar prompt de credenciais só quando necessário — senha nunca gravada em código/disco. Travas: confirmação dupla, lista negra de programas críticos, log de tudo.
- **Exportar inventário para Excel/CSV**: lista de máquinas com dados; útil como checklist da migração Kaspersky → Falcon. Baixo risco, alto valor.
- **Inventário de hardware detalhado**: SSD vs HD, RAM, CPU, placa-mãe via WMI. Estratégico para planejar upgrades.
- **WebView2 para painel de impressora embutido**: abrir o painel web da impressora dentro do sistema (`Microsoft.Web.WebView2`) em vez de navegador externo. Conceito aprovado, não implementado.
- **Busca de máquina por usuário logado**: usuário liga sem saber o nome da máquina; busca "maria" → acha onde está logada.

---

## Forma de trabalho do mantenedor (Vinicius)

- **Idioma:** português brasileiro, sempre.
- **Nível:** iniciante em C# que aprende fazendo. Explicar o "porquê", não só o "o quê". Evitar jargão sem explicação.
- **Entrega de código:** prefere **arquivos completos** para colar (apagar tudo e colar), especialmente em arquivos grandes ou que deram erro. Para arquivos grandes e estáveis, trechos "antes/depois" com ponto de inserção exato também funcionam.
- **Ritmo:** incremental, uma coisa de cada vez, testando entre cada etapa. Não despejar vários arquivos de uma vez. Ordem típica: Model → Service → Designer → eventos no MainForm, compilando (F6) entre cada etapa e rodando (F5) ao fim.
- **Aprovação visual:** mostrar mockups/layouts e aprovar o design **antes** de codar a interface.
- **Indicar ponto exato de inserção:** ao mandar trecho, dizer exatamente onde colar. Já houve bug de método duplicado por colar no lugar errado.
- **Confirmar nomes antes de assumir:** ao referenciar controles/variáveis existentes, confirmar o nome real (ex: `_listaMaquinas`) em vez de chutar.
- **Backup antes de mudanças grandes:** o mantenedor faz tag git, branch separada e cópia física antes de redesigns. Incentivar isso antes de mexidas arriscadas.
- **Prioridade em desempenho e leveza:** evitar complexidade desnecessária. Se uma abordagem for mais simples e mais leve, preferir ela mesmo que menos "elegante".
- **Honestidade técnica é valorizada:** dizer quando algo não vai funcionar 100% em vez de prometer. Tratar o mantenedor como parceiro técnico, não apenas executar pedidos cegamente.

---

## Ambiente do hospital (AMEB — Barra das Garças/MT)

- **Domínio AD:** `ameh.org.br` (atenção: `ameh`, não variações). Máquinas nomeadas como `AME-SETOR-NN` (ex: AME-ADM-10, AME-AGE-05). Setor detectado pelo nome.
- **Parque:** ~420 máquinas. Servidores filtrados pelo atributo `operatingSystem` do AD (contém "Server").
- **Servidor de impressão:** `\\islandia`. ~41 impressoras, maioria Lexmark. Monitoramento de toner via SNMP (OIDs Printer-MIB `1.3.6.1.2.1.43.*`, community "public", SNMP v1).
- **Antivírus:** migração em andamento de Kaspersky (legado, Kaspersky Security Center) para Falcon/CrowdStrike. Daí a necessidade de mapear o Kaspersky.
- **SALUX (ERP hospitalar):** havia problema de impressora padrão sendo trocada por Zebra após Windows 11 25H2. Existe app separado (PrinterGuard) que monitora e restaura a impressora correta (`\\islandia\PRT-SADT-01`). Cuidado com mudanças que afetem impressora padrão.
- **Synapse PACS (Fujifilm, imagens médicas):** versão 4.4.400, dependente de ActiveX/IE Mode. Abre via `http://amehdsweb`. Algumas estações dependem de IE Mode no Edge via GPO — não assumir navegador moderno em tudo.
- **Virtualização na máquina de TI:** a estação do mantenedor usa Hyper-V (e tem histórico de VirtualBox para lab). Daí o cuidado com detecção de IP local — a máquina pode ter múltiplos adaptadores virtuais.
- **Rede:** faixa `172.18.x.x`. Já houve incidente de roteador em modo router em vez de AP (causava isolamento NAT e falha de monitoramento/acesso remoto).
- **Permissões:** o TIHub roda com `requireAdministrator`. PSExec precisa de privilégios de admin de domínio para alcançar máquinas remotas. Em máquina offline, qualquer operação remota falha com "Erro 6 / Connecting" — comportamento esperado, não bug.