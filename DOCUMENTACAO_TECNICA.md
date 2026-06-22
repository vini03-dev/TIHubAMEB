# Documentação Técnica — TIHub AMEB v1.0

Este documento detalha a arquitetura interna, decisões de design e o funcionamento de cada componente do sistema.

---

## Índice

1. [Arquitetura geral](#arquitetura-geral)
2. [Models](#models)
3. [Services](#services)
4. [Helpers](#helpers)
5. [Fluxos principais](#fluxos-principais)
6. [Decisões de design](#decisões-de-design)

---

## Arquitetura geral

O projeto segue uma separação simples em três camadas:

```
MainForm (UI)  →  Services (lógica de negócio)  →  Models (dados)
```

- **Models**: classes de dados puras, sem lógica de negócio além de formatação e detecção simples.
- **Services**: toda a lógica de execução, comunicação com WMI/AD/PSExec/SNMP e regras de negócio.
- **MainForm**: apenas orquestra chamadas aos Services e atualiza a UI. Não contém lógica de negócio.

Toda operação potencialmente lenta (WMI, PSExec, ping, AD, SNMP) é `async` e roda fora da thread de UI.

---

## Models

### `MaquinaInfo`
Representa o estado em tempo real de uma máquina monitorada no Dashboard (CPU, RAM, disco, uptime, usuário logado). Usado tanto para máquina local quanto remota via WMI.

Contém o método estático `DetectarSetor(nomeMaquina)`, que extrai o setor a partir do nome da máquina (ex: `PC-INF-01` → `INF`).

### `MaquinaRede`
Representa uma máquina encontrada no Active Directory, usada na aba Máquinas. Populada a partir de consulta ao AD via `DirectoryServices`.

Campo `UsuarioConectado`: preenchido de forma assíncrona e independente do ping, via WMI, após verificação de status online — para não atrasar o ping em massa.

### `ImpressoraInfo`
Representa uma impressora cadastrada no `Data/impressoras.json`, combinada com seu status em tempo real (SNMP ou ping). Contém propriedades computadas como `TonerCritico` (≤15%), `TonerBaixo` (≤30%) e `TemAlerta` — usadas para ordenar automaticamente impressoras com problema no topo da lista.

### `LogEntry`
Estrutura de um registro de log. O enum `TipoLog` categoriza cada entrada (Info, Limpeza, Otimizacao, Rede, Maquinas, Usuarios, Sistema, Erro).

### `PerfilLimpeza`
Define quais itens serão limpos em uma operação. Contém três factory methods estáticos (`Leve()`, `Medio()`, `Avancado()`) que retornam presets pré-configurados.

### `UsuarioInfo`
Representa um perfil de usuário do Windows. A propriedade `Protegido` centraliza toda a lógica de proteção contra exclusão acidental de contas críticas.

---

## Services

### `LogService`
Camada central de logging. Grava em arquivo diário (`%AppData%/TIHubAMEB/Logs/yyyy-MM-dd.log`) e espelha cada entrada em um `RichTextBox` vinculado via `VincularControle()`. A coloração é decidida por `TipoLog` num switch expression simples.

### `PsExecService`
Camada de execução de comandos local ou remoto. Pontos importantes:
- Nunca abre janela visível (`CreateNoWindow = true`)
- Timeout configurável (120s) com `Task.WhenAny`
- Retry automático (2 tentativas)
- Caminho do `PsExec64.exe` resolvido via `AppDomain.CurrentDomain.BaseDirectory`

### `MonitoramentoService`
Coleta métricas de hardware. Local usa `PerformanceCounter` e `DriveInfo`. Remoto usa WMI. Implementa coleta assíncrona não bloqueante: `ObterInfoAtual()` retorna o último cache e dispara nova coleta em background.

### `MaquinasService`
Busca máquinas no Active Directory. Decisão de design relevante: **servidores são filtrados pelo atributo `operatingSystem` do AD** (contendo "Server"), não por nome de máquina — método mais confiável.

```csharp
if (so.Contains("Server", StringComparison.OrdinalIgnoreCase))
    continue; // ignora servidores
```

Cache de 5 minutos para não sobrecarregar o controlador de domínio. Ping em massa com `SemaphoreSlim(20)`. Coluna "Usuário Conectado" preenchida via WMI em background após o ping.

### `OtimizacaoService`
Aplica perfis de otimização. Ponto técnico mais importante: **resolução correta do contexto de registro do usuário**. Como PSExec executa como SYSTEM, escrita em `HKCU` afeta o perfil do SYSTEM, não do usuário real. O serviço detecta o SID do usuário logado e usa `HKEY_USERS\{SID}`:

```csharp
private static string CaminhoRegistro(string? sid, string subcaminho) =>
    string.IsNullOrEmpty(sid)
        ? $"HKCU:\\{subcaminho}"
        : $"Registry::HKEY_USERS\\{sid}\\{subcaminho}";
```

### `LimpezaService`
Executa limpezas via PowerShell em sequência, reportando progresso em tempo real. Suporta limpeza de temp, cache, prefetch, lixeira, DNS, navegadores, Windows Update, spool, dumps e componentes WinSxS.

### `RedeService`
Operações de rede e controle remoto. Inclui ping, compartilhamento C$, flush DNS, reinício de Explorer, reinício/desligamento agendado, PowerShell remoto, RDP (`mstsc.exe /v:{maquina}`) e Assistência Remota (`msra.exe` com fallback de clipboard).

### `SistemaService`
Executa SFC e DISM com parsing heurístico da saída para apresentar resultado legível (ex: "✓ Nenhum problema encontrado").

> Limitação: SFC exige sessão interativa para diagnóstico completo. Via PSExec (SYSTEM não-interativo) pode não retornar dados completos.

### `UsuarioService`
Lista perfis via `Win32_UserProfile` (WMI) e implementa exclusão de pasta de perfil usando `takeown` + `icacls` + `Remove-Item` para garantir permissão mesmo em pastas com ACLs restritivas. **Nunca exclui entradas do registro** — decisão deliberada de segurança.

### `ImpressoraService`
Carrega cadastro do arquivo externo `Data/impressoras.json`. Para cada impressora:

1. Tenta SNMP (porta 161, UDP, community `public`) usando Printer-MIB padrão (RFC 1759)
2. Se SNMP não responder, cai para ping simples
3. Calcula nível de toner como percentual: `(atual / max) * 100`

OIDs utilizados:
- `1.3.6.1.2.1.43.11.1.1.9.1.1` — nível de toner atual
- `1.3.6.1.2.1.43.11.1.1.8.1.1` — capacidade máxima de toner
- `1.3.6.1.2.1.43.10.2.1.4.1.1` — contador de páginas

Impressoras com alerta (offline ou toner crítico) são automaticamente ordenadas no topo da lista via `OrdenarComAlertasNoTopo()`.

O arquivo `Data/impressoras.json` está no `.gitignore` — cada instalação tem seu próprio arquivo com os dados reais da rede local. O repositório contém apenas `impressoras.exemplo.json` com dados fictícios.

### `DistribuicaoService`
Copia arquivos para o Desktop Público de múltiplas máquinas simultaneamente via compartilhamento `C$`. Não usa PSExec — acesso direto pela rede administrativa.

Detecta automaticamente o idioma da pasta Desktop:
```csharp
string desktopPT = Path.Combine(raiz, "Área de Trabalho");
string desktopEN = Path.Combine(raiz, "Desktop");

if (Directory.Exists(desktopPT)) destino = desktopPT;
else if (Directory.Exists(desktopEN)) destino = desktopEN;
```

Usa `SemaphoreSlim(15)` para limitar operações paralelas e não saturar a rede.

---

## Helpers

### `UIHelper`
Centraliza paleta de cores, fontes e métodos de estilização para controles Guna.UI2. Não contém lógica de negócio — existe para manter consistência visual e evitar repetição de cores hard-coded.

---

## Fluxos principais

### Fluxo de monitoramento de impressoras

```
Aba Impressoras abre
  → ImpressoraService.CarregarCadastroAsync()
    → lê Data/impressoras.json
    → popula cards na UI (sem status ainda)

Usuário clica "Atualizar Tudo"
  → ImpressoraService.VerificarTodasAsync()
    → SemaphoreSlim(15) + paralelo por impressora
    → TentarSnmpAsync() — tenta SNMP primeiro
      → se ok: nível de toner + contador de páginas
      → se falha: PingAsync() — só online/offline
  → OrdenarComAlertasNoTopo() — alertas sobem
  → UI redesenha os cards com cores e barras
```

### Fluxo de distribuição de arquivos

```
Usuário seleciona máquinas na lista (Ctrl+Clique)
→ Clica "Distribuir Arquivos"
→ OpenFileDialog — escolhe os arquivos
→ Confirmação com resumo
→ DistribuicaoService.DistribuirAsync()
  → SemaphoreSlim(15) + paralelo por máquina
  → Detecta Desktop PT ou EN via C$
  → File.Copy() com overwrite: true
→ Resultado: X sucesso / Y falhou
```

### Fluxo de otimização remota com SID correto

```
Usuário clica "Aplicar Otimização" para máquina remota
→ OtimizacaoService.AplicarPerfilAsync()
  → ObterSidUsuarioLogado() via WMI (explorer.exe owner)
  → CaminhoRegistro(sid, subcaminho)
    → "Registry::HKEY_USERS\{SID}\..." se remoto
    → "HKCU:\..." se local
  → PsExecService.ExecutarPowerShellAsync() por etapa
→ Ajustes chegam no perfil do usuário real, não do SYSTEM
```

---

## Decisões de design

1. **Sem exclusão de e-mail/Exchange**: risco de impacto amplo em servidor Exchange de produção hospitalar.

2. **Sem exclusão de registro de usuário**: risco de deixar Windows em estado inconsistente.

3. **Filtro de servidor por atributo do AD**: nomenclatura de máquina não é fonte confiável. O atributo `operatingSystem` do AD é authoritative.

4. **PSExec embutido via caminho relativo**: facilita distribuição sem configuração manual.

5. **Impressoras em arquivo externo**: IPs e dados de rede ficam fora do repositório público, cada instalação tem seu próprio `impressoras.json`.

6. **SNMP com fallback para ping**: o painel de impressoras funciona mesmo sem SNMP habilitado — mostra pelo menos o status online/offline.
