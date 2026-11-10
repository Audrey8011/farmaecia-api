# Farma&Cia — API ASP.NET Core

API REST que atende o app Flutter da Farma&Cia. Ela lê e grava no mesmo banco SQL Server usado pelo projeto MVC da loja, então produtos, clientes e pedidos ficam sincronizados entre as duas interfaces.

## Endereços publicados

- API em execução (Azure App Service, plano gratuito F1): https://farmaecia-api-audrey-2026.azurewebsites.net/api/produtos
- Loja MVC (Azure): https://farmaecia-mvc-audrey-2026.azurewebsites.net/
- App Flutter: https://github.com/Audrey8011/farmaecia-flutter
- Vitrine estática (GitHub Pages): https://audrey8011.github.io/farmaecia/

## Tecnologias

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 com SQL Server
- Autenticação JWT Bearer
- Hash de senhas com `PasswordHasher<T>` do ASP.NET Identity

## Endpoints

| Método | Rota | Autenticação | Descrição |
|---|---|---|---|
| GET | `/api/produtos` | pública | Lista os produtos ativos, ordenados por categoria e nome |
| POST | `/api/auth/registrar` | pública | Cadastra um cliente e devolve um token JWT |
| POST | `/api/auth/login` | pública | Autentica por e-mail e senha e devolve um token JWT |
| POST | `/api/pedidos` | JWT | Cria um pedido, valida estoque e baixa as quantidades |

Exemplos de corpo:

```json
// POST /api/auth/login
{ "email": "cliente@exemplo.com", "senha": "Senha@123" }

// POST /api/pedidos
{ "itens": [ { "produtoId": 1, "quantidade": 2 } ] }
```

A resposta do login/registro tem o formato:

```json
{ "token": "eyJ...", "usuarioId": 1, "nome": "Cliente", "email": "cliente@exemplo.com" }
```

## Como executar localmente

1. Tenha o .NET 10 SDK e o SQL Server LocalDB instalados.
2. Ajuste a conexão em `Farmacia_site/appsettings.Development.json`, se necessário:

   ```json
   "ConnectionStrings": {
     "FarmaECiaDb": "Server=(localdb)\\MSSQLLocalDB;Database=FarmaECiaDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. **O schema do banco é criado pelo projeto MVC Farma&Cia** (`InicializadorBanco`, com `EnsureCreatedAsync` e os dados iniciais). Rode o MVC uma vez apontando para o mesmo banco antes de usar a API: https://github.com/Audrey8011/farmaecia
4. Execute a API:

   ```powershell
   cd Farmacia_site
   dotnet run
   ```

   A API sobe em `http://localhost:5178`. Teste com:

   ```powershell
   Invoke-RestMethod http://localhost:5178/api/produtos
   ```

## Configuração de produção

Nenhuma credencial real fica no repositório. Em produção, configure no App Service (ou em variáveis de ambiente):

- `ConnectionStrings__FarmaECiaDb`: conexão do SQL Server/Azure SQL;
- `Jwt__Key`: chave secreta aleatória com pelo menos 32 bytes;
- `Jwt__Issuer` e `Jwt__Audience`: emissor e público do token;
- `Cors__AllowedOrigins__0`: origem HTTPS do app Flutter Web publicado;
- `ASPNETCORE_ENVIRONMENT`: `Production`.

Para publicar uma atualização:

```powershell
cd Farmacia_site
dotnet publish -c Release -o ..\publish
Compress-Archive -Path ..\publish\* -DestinationPath ..\api.zip -Force
az webapp deploy -g rg-farmaecia-student -n farmaecia-api-audrey-2026 --src-path ..\api.zip --type zip
```

## Estrutura

```
Farmacia_site/
├── Controllers/ApiController.cs   # produtos, autenticação e pedidos
├── Data/Contexto.cs               # mapeamento EF Core
├── Models/Entidades.cs            # Produto, Usuario, Pedido, ItemPedido
├── Program.cs                     # JWT, CORS e pipeline
└── appsettings*.json              # configurações locais
```

## Segurança

- Senhas são armazenadas apenas como hash.
- O token JWT expira em 8 horas.
- Em produção, use HTTPS, uma `Jwt__Key` exclusiva e nunca versione a connection string.
- A conta de administrador de demonstração do banco é criada pelo MVC (`admin@farmaecia.local`); troque a senha antes de usar em produção.
