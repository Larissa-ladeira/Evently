# Evently - Plataforma de Gestão de Eventos e Ingressos

Plataforma completa para criar eventos, vender ingressos e gerenciar check-ins.

## Tecnologias

- **Backend**: C# .NET 10 Web API, Entity Framework Core + PostgreSQL, MongoDB, Redis, JWT
- **Frontend**: Vue 3 + TypeScript + Pinia + Vue Router + Axios
- **Infra**: Docker Compose

## Execução Rápida

```bash
docker compose up -d
cd Evently.Api && dotnet ef database update && dotnet run
cd Evently.Api/evently-web && npm install && npm run dev
```

Acesse:
- API Swagger: http://localhost:5072/swagger
- Frontend: http://localhost:5173
- MailHog: http://localhost:8025

> O PostgreSQL do Docker é exposto na porta **5433** (internamente 5432), para não
> conflitar com um PostgreSQL nativo que porventura já use a 5432 na máquina.

## Configuração de segredos

Nenhum segredo fica nos arquivos `appsettings`. A API falha no arranque se algum
estiver ausente, inválido ou com menos de 32 caracteres. Configure via User
Secrets em desenvolvimento:

```bash
cd Evently.Api
dotnet user-secrets set "Jwt:Key" "<chave-aleatoria-com-32+-caracteres>"
dotnet user-secrets set "Payment:WebhookSecret" "<outro-segredo-com-32+-caracteres>"
```

Em produção, use as variáveis de ambiente `Jwt__Key` e
`Payment__WebhookSecret` (o `__` representa a seção aninhada).

O segredo do webhook valida o cabeçalho
`X-Evently-Signature: sha256=<HMAC_SHA256_do_corpo_em_hex>` em
`POST /api/Payments/webhook`.

## Contas criadas automaticamente (seed)

O backend aplica as migrações e cria utilizadores na primeira execução:

- Admin: `admin@evently.com` / `Admin@123`
- Portaria (Staff): `staff@evently.com` / `Staff@123`

## Funcionalidades

- Autenticação JWT com senhas protegidas por BCrypt
- Eventos, lotes de ingressos, pedidos, pagamento mock e webhook HMAC idempotente
- Preço calculado no servidor e estoque transacional com lock Redis (falha rápido se o Redis estiver indisponível)
- Formulários dinâmicos de inscrição por evento (MongoDB), validados no checkout
- Geração de QR codes e envio de ingressos por e-mail (MailHog)
- Check-in com registo de logs (MongoDB) e relatórios de vendas/receita/check-ins

## Testes e validação

```bash
dotnet test
cd Evently.Api/evently-web && npm run build
```
