# Roteiro de Testes Manuais — TIHub AMEB v1.0

Use este checklist para validar o sistema antes de cada release. Recomenda-se testar em máquina de homologação isolada antes de usar em produção.

> `PC-TESTE-01` representa qualquer máquina de teste do seu domínio.

---

## Pré-requisitos

- [ ] `PsExec64.exe` presente em `Tools/`
- [ ] `Data/impressoras.json` preenchido com dados reais
- [ ] Aplicação executando como Administrador
- [ ] Conta de domínio com permissão administrativa em pelo menos 2 máquinas de teste

---

## 1. Dashboard

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 1.1 | Abrir sem máquina remota | Dados do PC local aparecem | ☐ |
| 1.2 | Aguardar 5+ segundos | CPU/RAM atualizam automaticamente | ☐ |
| 1.3 | Monitorar máquina remota (aba Rede → Monitorar) | Dashboard muda para REMOTO | ☐ |
| 1.4 | Máquina remota offline | Não trava; exibe status offline | ☐ |

---

## 2. Limpeza

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 2.1 | Preset Leve localmente | Progresso avança, log mostra etapas | ☐ |
| 2.2 | Preset Médio | Inclui cache de navegador | ☐ |
| 2.3 | Preset Avançado | Inclui DISM Cleanup | ☐ |
| 2.4 | Modo personalizado | Só itens marcados executam | ☐ |
| 2.5 | Cancelar em andamento | Para imediatamente, log registra cancelamento | ☐ |
| 2.6 | Limpeza em máquina remota | Mesmo resultado via PSExec | ☐ |

---

## 3. Otimização

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 3.1 | Perfil Escritório localmente | Plano de energia muda para Balanceado | ☐ |
| 3.2 | Perfil Hospital | Só ajustes conservadores | ☐ |
| 3.3 | Perfil Ultra Performance | Serviços SysMain/WSearch desativados | ☐ |
| 3.4 | Otimização em máquina remota com usuário logado | Efeitos visuais chegam no perfil do usuário real (não SYSTEM) | ☐ |
| 3.5 | Máquina remota sem usuário logado | Log avisa; só ajustes de máquina aplicados | ☐ |

---

## 4. Rede

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 4.1 | Ping em máquina online | Retorna ms de resposta | ☐ |
| 4.2 | Ping em máquina offline | "Sem resposta" sem travar | ☐ |
| 4.3 | Abrir C$ | Explorer abre na pasta remota | ☐ |
| 4.4 | Flush DNS | Comando executa sem erro | ☐ |
| 4.5 | Reiniciar Explorer | Explorer reinicia na máquina remota | ☐ |
| 4.6 | Reiniciar máquina | Confirmação aparece antes de executar | ☐ |
| 4.7 | Cancelar shutdown | Cancela agendamento anterior | ☐ |
| 4.8 | PowerShell remoto | Janela abre, comando retorna resultado | ☐ |
| 4.9 | RDP | mstsc.exe abre com nome preenchido | ☐ |
| 4.10 | Assistência Remota | msra.exe abre; IP copiado para clipboard | ☐ |
| 4.11 | Gerenciamento | compmgmt.msc abre apontando para máquina remota | ☐ |

---

## 5. Máquinas

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 5.1 | Buscar no AD | Lista popula com estações de trabalho | ☐ |
| 5.2 | Confirmar que nenhum servidor aparece | Nenhuma máquina com SO "Windows Server" | ☐ |
| 5.3 | Buscar novamente dentro de 5 min | Usa cache (resposta instantânea) | ☐ |
| 5.4 | Filtro por texto em tempo real | Lista filtra enquanto digita | ☐ |
| 5.5 | Filtro por setor | Só máquinas do setor selecionado | ☐ |
| 5.6 | Filtro Online/Offline | Alterna Todos → Online → Offline → Todos | ☐ |
| 5.7 | Verificar Status | Ping em massa sem travar a UI | ☐ |
| 5.8 | Coluna Usuário Conectado | Mostra usuário logado nas online | ☐ |
| 5.9 | Duplo clique em máquina | Navega para aba Rede com nome preenchido | ☐ |
| 5.10 | Distribuir Arquivos — selecionar máquinas (Ctrl+Clique) | Seleção múltipla funciona | ☐ |
| 5.11 | Distribuir Arquivos — escolher arquivo e confirmar | Arquivo copiado para Desktop Público das máquinas selecionadas | ☐ |
| 5.12 | Distribuir em máquina com Windows PT | Copia para "Área de Trabalho" | ☐ |
| 5.13 | Distribuir em máquina com Windows EN | Copia para "Desktop" | ☐ |

---

## 6. Impressoras

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 6.1 | Abrir aba | Cards carregam com nome, localização, modelo e IP | ☐ |
| 6.2 | Clicar "Atualizar Tudo" | Sistema consulta SNMP ou ping em cada impressora | ☐ |
| 6.3 | Impressora com SNMP respondendo | Barra de toner e % aparecem corretamente | ☐ |
| 6.4 | Impressora sem SNMP | Status online/offline via ping; toner mostra "—" | ☐ |
| 6.5 | Impressora com toner crítico (≤15%) | Card com borda vermelha sobe para o topo | ☐ |
| 6.6 | Impressora offline | Card com borda vermelha sobe para o topo | ☐ |
| 6.7 | Filtro por texto | Cards filtram em tempo real | ☐ |
| 6.8 | Botão Painel | Abre http://IP-da-impressora no navegador | ☐ |
| 6.9 | Botão Fila | Abre \\servidor\NomeDaImpressora no Explorer | ☐ |
| 6.10 | Data/impressoras.json ausente | Log orienta a copiar o arquivo de exemplo | ☐ |

---

## 7. Usuários

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 7.1 | Listar local | Mostra perfis exceto contas de sistema | ☐ |
| 7.2 | Listar remoto | Mesmo resultado via WMI | ☐ |
| 7.3 | Excluir usuário logado | Bloqueado com mensagem de proteção | ☐ |
| 7.4 | Excluir Administrator | Bloqueado com mensagem de proteção | ☐ |
| 7.5 | Excluir perfil inativo | Pasta removida do disco com sucesso | ☐ |
| 7.6 | Filtro por nome | Lista filtra em tempo real | ☐ |

---

## 8. Sistema

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 8.1 | DISM CheckHealth | Resultado em ~1 min, interpretado de forma legível | ☐ |
| 8.2 | DISM ScanHealth | Completa sem travar a interface | ☐ |
| 8.3 | SFC /scannow local | Resultado completo quando rodado localmente | ☐ |
| 8.4 | DISM RestoreHealth | Conclui sem erro em imagem saudável | ☐ |
| 8.5 | Verificação Completa | Progresso avança por cada etapa | ☐ |

---

## 9. Logs

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 9.1 | Executar qualquer ação | Entrada colorida aparece no log | ☐ |
| 9.2 | Abrir pasta | Explorer abre em %AppData%/TIHubAMEB/Logs/ | ☐ |
| 9.3 | Verificar arquivo do dia | Contém todas as entradas da sessão | ☐ |
| 9.4 | Limpar tela | Tela limpa; arquivo em disco permanece | ☐ |

---

## 10. Robustez geral

| # | Teste | Esperado | OK? |
|---|-------|----------|-----|
| 10.1 | Ação remota com máquina inválida | Erro tratado, sem crash | ☐ |
| 10.2 | Ação remota sem permissão | Erro de acesso negado logado | ☐ |
| 10.3 | Fechar durante operação longa | Encerra sem travar o sistema | ☐ |
| 10.4 | Sem conexão com AD | Busca falha com mensagem clara no log | ☐ |
| 10.5 | PsExec64.exe ausente | Mensagem clara orientando onde baixar | ☐ |

---

## Registro de execução

| Data | Versão | Testador | Observações |
|------|--------|----------|-------------|
| | v1.0 | | |
