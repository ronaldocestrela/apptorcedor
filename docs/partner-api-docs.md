# 📖 Documentação — API Partner Lookup (Verificação de Sócio)

## 🎯 O que é?

A **API Partner Lookup** permite que plataformas externas (como sua loja, site de vendas, etc.) verifiquem, através de um número de telefone, se um cliente cadastrado na plataforma AppTorcedor é um **sócio ativo**.

Essa informação pode ser usada para:
- ✅ Aplicar descontos automáticos na compra
- ✅ Oferecer benefícios exclusivos
- ✅ Personalizar a experiência do cliente
- ✅ Validar elegibilidade para promoções

---

## 🔐 Autenticação — Obtendo sua Chave de API

Antes de usar a API, você precisa solicitar uma **Chave de API** ao administrador do AppTorcedor.

### Passo 1: Gerar a chave no painel administrativo
No backoffice, acesse **Sistema → Integrações — Tokens** (`/admin/webhook-tokens`).

Permissões necessárias:
- `Webhooks.Visualizar`: listar tokens existentes
- `Webhooks.Gerenciar`: gerar e revogar tokens

O token em texto claro é exibido apenas no momento da criação.

### Passo 2: Guardar a Chave com Segurança
Você receberá uma chave com este formato:
```
sk_partner_ABCDE1234567890XYZ...
```

⚠️ **IMPORTANTE:**
- Guarde essa chave **em um lugar seguro**
- Nunca compartilhe em código público ou em logs
- Se comprometer, peça ao admin para revogar imediatamente
- Se perder o token, gere um novo e revogue o antigo

---

## 🔍 Como Usar — Passo a Passo

### Requisição Básica

```bash
GET https://seu-dominio.com/api/partner/v1/lookup?phone=11999999999

Headers:
  X-Api-Key: sk_partner_ABCDE1234567890XYZ...
```

### Explicação do que enviar

| Campo | O que é | Exemplo |
|-------|---------|---------|
| **phone** | Número de telefone do cliente | `11999999999` (só dígitos) ou `(11) 99999-9999` (formatado) |
| **X-Api-Key** | Sua chave de API | `sk_partner_ABC...` |

O API aceita telefone em qualquer formato — com parênteses, hífens, espaços, DDI (+55), etc. O importante é conter o número.

---

## 📨 Resposta da API

### Sucesso (HTTP 200)

```json
{
  "exists": true,
  "isActiveMember": true
}
```

| Campo | Significado |
|-------|-------------|
| **exists** | `true` = telefone cadastrado no sistema; `false` = não encontrado |
| **isActiveMember** | `true` = é sócio ativo e pode usufruir de benefícios; `false` = existe mas não é sócio ativo |

### Exemplo: Cliente é Sócio
```json
{
  "exists": true,
  "isActiveMember": true
}
→ Aplica desconto de 10% ✅
```

### Exemplo: Cliente Existe Mas Não é Sócio
```json
{
  "exists": true,
  "isActiveMember": false
}
→ Mostra mensagem: "Você não é sócio ainda. Conheça os benefícios!" 💡
```

### Exemplo: Cliente Não Encontrado
```json
{
  "exists": false,
  "isActiveMember": false
}
→ Sem desconto, trata como cliente comum
```

---

## ⚠️ Erros — O que Pode Dar Errado

### Erro 400 — Telefone Vazio ou Inválido
```
Status: 400 Bad Request

Causa: Você não enviou o parâmetro `phone`

Solução: Inclua `?phone=11999999999` na URL
```

### Erro 401 — Chave de API Inválida
```
Status: 401 Unauthorized

Causa: 
- Chave expirada ou revogada
- Chave escrita errada
- Chave não enviada no header

Solução: 
- Verifique se a chave está completa e correta
- Peça ao admin para criar uma nova
- Verifique se está no header: X-Api-Key
```

### Erro 500 — Erro do Servidor
```
Status: 500 Internal Server Error

Causa: Problema técnico no servidor

Solução: 
- Espere alguns minutos e tente novamente
- Se persistir, avise ao administrador
```

---

## 💻 Exemplos de Integração

### JavaScript / Node.js

```javascript
const apiKey = 'sk_partner_ABCDE1234567890XYZ...'; // Sua chave
const phone = '11999999999'; // Telefone do cliente

fetch(`https://seu-dominio.com/api/partner/v1/lookup?phone=${phone}`, {
  method: 'GET',
  headers: {
    'X-Api-Key': apiKey
  }
})
  .then(res => res.json())
  .then(data => {
    if (data.exists && data.isActiveMember) {
      console.log('✅ Cliente é sócio ativo — aplicar desconto!');
    } else if (data.exists) {
      console.log('⏳ Cliente existe mas não é sócio');
    } else {
      console.log('❌ Cliente não encontrado');
    }
  })
  .catch(err => console.error('Erro:', err));
```

### Python

```python
import requests

api_key = 'sk_partner_ABCDE1234567890XYZ...'
phone = '11999999999'

headers = {'X-Api-Key': api_key}
response = requests.get(
    'https://seu-dominio.com/api/partner/v1/lookup',
    params={'phone': phone},
    headers=headers
)

if response.status_code == 200:
    data = response.json()
    if data['exists'] and data['isActiveMember']:
        print('✅ Aplicar desconto!')
    else:
        print('Sem desconto para este cliente')
else:
    print(f'Erro: {response.status_code}')
```

### cURL (para testes rápidos)

```bash
curl -X GET \
  'https://seu-dominio.com/api/partner/v1/lookup?phone=11999999999' \
  -H 'X-Api-Key: sk_partner_ABCDE1234567890XYZ...'
```

---

## ✅ Boas Práticas

### 1️⃣ Nunca Exponha a Chave
❌ Evite:
```javascript
// ERRADO! Nunca faça isso!
const api = fetch('/api?key=sk_partner_ABC...');
```

✅ Use:
```javascript
// Certo! A chave fica no servidor
fetch(`/seu-backend/verificar-socio?phone=${phone}`)
```

### 2️⃣ Trate Erros Gracefully
```javascript
try {
  const response = await fetch(...);
  if (response.status === 401) {
    console.log('Chave inválida — contato com admin');
  } else if (!response.ok) {
    console.log('API temporariamente indisponível');
    // Usa preço normal, sem desconto
  }
} catch (err) {
  // Conectividade offline — valor padrão
}
```

### 3️⃣ Cache Resultados (Opcional)
Se você faz muitas consultas do mesmo cliente, guarde o resultado por um tempo (ex.: 1 hora) para reduzir requisições.

```javascript
const cache = new Map();
const CACHE_TIME = 3600000; // 1 hora em ms

async function verificarSocio(phone) {
  const agora = Date.now();
  
  // Verifica cache
  if (cache.has(phone)) {
    const [resultado, tempo] = cache.get(phone);
    if (agora - tempo < CACHE_TIME) return resultado;
  }
  
  // Se expirou, consulta API
  const resultado = await fetch(...).then(r => r.json());
  cache.set(phone, [resultado, agora]);
  return resultado;
}
```

### 4️⃣ Não Armazene Dados Pessoais
A API **não retorna** nome, email ou outros dados — apenas `true`/`false`. Isso protege a privacidade. **Não tente guardar ou deduzir informações adicionais**.

---

## 📞 Suporte e Dúvidas

Se tiver dúvidas:
1. Releia esta documentação
2. Teste com o cURL acima
3. Contate o **administrador do AppTorcedor** 

---

## 📋 Checklist de Implementação

Antes de colocar em produção:

- [ ] Chave de API solicitada ao admin
- [ ] Chave armazenada com segurança (variáveis de ambiente, não em código)
- [ ] Código trata erros 401 (chave inválida)
- [ ] Código trata erros 500 (servidor indisponível)
- [ ] Testado com telefone conhecido (sócio ativo)
- [ ] Testado com telefone desconhecido
- [ ] Interface do usuário mostra desconto corretamente
- [ ] Logs não contêm a chave de API
- [ ] Documentação interna da empresa atualizada

---

## 🎓 Glossário

| Termo | Explicação |
|-------|-----------|
| **API** | Interface que permite sua aplicação conversar com outra |
| **API Key / Chave de API** | Senha que prova sua identidade e permissão de usar a API |
| **HTTP 200** | Tudo ok, resposta recebida com sucesso |
| **HTTP 400** | Você enviou algo errado (ex.: sem telefone) |
| **HTTP 401** | Você não tem permissão (chave errada/expirada) |
| **HTTP 500** | Erro do servidor (problema deles, não seu) |
| **Header** | Informação extra que vai junto da requisição |
| **JSON** | Formato de dados que a API usa para responder |

---

**Versão**: 1.0  
**Data**: 12 de Maio de 2026  
**Mantido por**: Time AppTorcedor
