# Gerar e configurar chave SSH (VPS e Jenkins)

Este guia explica **onde** gerar o par de chaves, **passo a passo**, como autorizar a chave **pública** na VPS e como guardar a **privada** no **Jenkins** — alinhado ao deploy em [`guia-deploy.md`](guia-deploy.md).

> **Jenkins na mesma VPS que a aplicação:** com **`JENKINS_LOCAL_DEPLOY=true`** (padrão no `Jenkinsfile`), o pipeline **não** usa `ssh`/`scp` nem credenciais `vps-ssh-key` / `vps-host`. Só precisas deste guia se **`JENKINS_LOCAL_DEPLOY=false`** (agente remoto).

---

## Onde gerar a chave: localmente ou na VPS?

| Onde gerar | Recomendação | Motivo |
|------------|----------------|--------|
| **No teu PC (Linux, macOS ou WSL)** ou **no servidor Jenkins** (controlador/agente que executa `ssh`/`scp`) | **Sim — é o fluxo recomendado** | A **chave privada** deve ficar só no **cliente** que se liga por SSH (tu, para testar; Jenkins, para o deploy). A **VPS só precisa da chave pública** num ficheiro. |
| **Na própria VPS** | **Não é o fluxo típico** para Jenkins | Se gerares o par **dentro** da VPS, a privada nasce no servidor; para o Jenkins usar essa mesma chave terias de **copiar a privada da VPS para o Jenkins**, o que é mais trabalhoso e confuso. Só faz sentido em cenários muito específicos (ex.: automatização só **dentro** da VPS). |

**Resumo:** gera o par **localmente** (ou na máquina onde o Jenkins vai usar a chave). **Não** precisas de gerar nada “dentro da VPS” para o deploy habitual: na VPS apenas **instalas a chave pública** (e opcionalmente crias o utilizador Linux).

---

## Visão rápida do fluxo

1. **Gerar** par (privada + `.pub`) **no PC ou no host Jenkins** →  
2. Colar **só a `.pub`** na VPS (`authorized_keys`) →  
3. Testar `ssh` a partir do mesmo sítio onde geraste →  
4. Colar a **privada** na credencial do Jenkins (se o deploy for pelo Jenkins noutra máquina, a privada tem de estar **no Jenkins**, não só no teu PC).

---

## Passo a passo completo (recomendado: gerar no teu computador)

### Passo 1 — Escolher o sítio de geração

- **Desenvolvimento / primeiro teste:** gera no **teu PC** (terminal Linux, macOS ou WSL no Windows).
- **Só Jenkins:** podes gerar **diretamente no servidor Jenkins** (SSH ao Jenkins e `ssh-keygen` lá), **ou** gerar no PC e depois copiar a **privada** para a credencial Jenkins — o importante é que o par usado pelo pipeline seja o mesmo cuja **pública** está na VPS.

### Passo 2 — Criar o par de chaves (no PC ou no Jenkins)

Num terminal **Linux**, **macOS** ou **WSL** (ou no host Jenkins, mesmo comando):

```bash
ssh-keygen -t ed25519 -C "deploy-apptorcedor" -f ~/.ssh/apptorcedor_deploy_ed25519
```

- **`-C`**: comentário (identifica a chave).
- **`-f`**: nome dos ficheiros (evita sobrescrever a chave default `id_ed25519`).

Quando pedir **passphrase**:

- **Vazio** — mais simples para automação (Jenkins); quem tiver o ficheiro da privada pode usar a chave.
- **Com passphrase** — mais seguro em disco; o Jenkins exige configuração extra (ssh-agent ou credencial que suporte passphrase).

Ficheiros criados:

- `~/.ssh/apptorcedor_deploy_ed25519` → **privada** (não partilhar, não commitar)
- `~/.ssh/apptorcedor_deploy_ed25519.pub` → **pública** (vai para a VPS)

**Alternativa (RSA antigo):**

```bash
ssh-keygen -t rsa -b 4096 -C "deploy-apptorcedor" -f ~/.ssh/apptorcedor_deploy_rsa
```

### Passo 3 — Ajustar permissões (Linux/macOS/WSL)

```bash
chmod 600 ~/.ssh/apptorcedor_deploy_ed25519
chmod 644 ~/.ssh/apptorcedor_deploy_ed25519.pub
```

### Passo 4 — Obter o texto da chave pública

```bash
cat ~/.ssh/apptorcedor_deploy_ed25519.pub
```

Copia a **linha inteira** (começa por `ssh-ed25519` ou `ssh-rsa`).

### Passo 5 — Na VPS: escolher o utilizador Linux do deploy

Define qual utilizador vai receber deploys (ex.: `deploy`, `ubuntu`, `debian`, `root`). Esse mesmo nome será o **Username** na credencial Jenkins.

### Passo 6 — Na VPS: instalar a chave pública

Tens de ter **um acesso inicial** à VPS (password de root, consola do cloud, ou outra chave já existente).

**Opção A — `ssh-copy-id` a partir do teu PC** (se já consegues SSH com password ou outra chave):

```bash
ssh-copy-id -i ~/.ssh/apptorcedor_deploy_ed25519.pub USUARIO@IP_OU_HOSTNAME
```

Substitui `USUARIO` e `IP_OU_HOSTNAME` (só hostname ou IP — **sem** `https://`).

**Opção B — manualmente na VPS:** entra na VPS (consola ou SSH), como o utilizador do deploy:

```bash
mkdir -p ~/.ssh
chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys
```

Cola **uma linha** por chave pública, guarda, e:

```bash
chmod 600 ~/.ssh/authorized_keys
```

### Passo 7 — Testar a partir do PC onde geraste a chave

```bash
ssh -i ~/.ssh/apptorcedor_deploy_ed25519 USUARIO@IP_OU_HOSTNAME
```

Se entrar **sem** pedir password (ou só passphrase da chave, se configuraste), está correto.

**Lembrete:** o destino do SSH é `USUARIO@host` — **não** uses URL web (`https://...`).

### Passo 8 — Jenkins: credencial com a chave privada

1. **Manage Jenkins** → **Credentials** → **Add Credentials**.
2. Tipo: **SSH Username with private key**.
3. **Username** = o mesmo `USUARIO` Linux da VPS (Passo 5).
4. **Private Key** = conteúdo **completo** do ficheiro **privado** (`apptorcedor_deploy_ed25519`), com:

   - `-----BEGIN OPENSSH PRIVATE KEY-----` (ou equivalente RSA)
   - todas as linhas
   - `-----END ... KEY-----`

   Podes abrir no editor e colar em **Enter directly**, ou usar ficheiro no controlador Jenkins.

5. **ID** da credencial: o esperado pelo [`Jenkinsfile`](../../Jenkinsfile) (ex.: `vps-ssh-key`).

6. Credencial **`vps-host`** (ou equivalente): só **hostname ou IP** da VPS — **sem** `https://`.

### Passo 9 — Primeiro deploy pelo Jenkins

Corre o job de deploy. O agente Jenkins usa a **privada** da credencial para o mesmo par cuja **pública** está na VPS.

---

## Se gerares o par na VPS (não recomendado para este projeto)

Só para referência: se alguém executar `ssh-keygen` **dentro** da VPS, os ficheiros ficam em `~/.ssh/` **desse** servidor. Para o Jenkins usar essa chave, terias de:

1. Copiar com segurança a **privada** do servidor para a credencial Jenkins (e apagar cópias temporárias), **ou**
2. Copiar só a **pública** para `authorized_keys` de outro user e manter a privada noutro sítio — facilmente inconsistente.

Por isso, para **Jenkins + VPS**, o fluxo documentado acima (gerar **fora** da VPS, pública na VPS, privada no Jenkins) é o mais claro e seguro.

---

## O que é cada ficheiro (referência)

| Ficheiro | Onde fica | Quem pode ver |
|----------|-----------|----------------|
| **Chave privada** | PC de dev e/ou credencial Jenkins | Só tu e o Jenkins; **nunca** no repositório Git |
| **Chave pública** (`.pub`) | Cópia na VPS em `~/.ssh/authorized_keys` | Pode ser pública; só “tranca” a porta com quem tem a privada correspondente |

---

## Erros comuns

| Sintoma | Causa provável |
|---------|----------------|
| `Load key "...": error in libcrypto` | Privada truncada, CRLF (Windows), paste incompleto no Jenkins |
| `Could not resolve hostname https` | Variável de host com URL (`https://...`) em vez de só hostname/IP |
| `Permission denied (publickey)` | `.pub` não está no `authorized_keys` do **mesmo** utilizador; par errado |

---

## Segurança — resumo

- Não faças **commit** da chave **privada**; o [`.gitignore`](../../.gitignore) ajuda com `*.key` / `*.pem`, mas chaves em `~/.ssh/` não devem ir para o repo.
- Rotação: novo par → nova linha em `authorized_keys` → atualizar Jenkins → remover linha antiga.

---

## Referências no repositório

- Deploy Jenkins + VPS: [`guia-deploy.md`](guia-deploy.md).
- Pipeline: [`Jenkinsfile`](../../Jenkinsfile).
