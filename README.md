# 🏥 ClinicApp

Sistema completo de gestão para clínicas, desenvolvido com .NET 8 e Blazor WebAssembly, oferecendo controle de estoque, gestão financeira e administração de múltiplas unidades.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat&logo=blazor)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?style=flat&logo=sqlite)
![License](https://img.shields.io/badge/license-MIT-green)

## 📋 Sobre o Projeto

ClinicApp é uma solução integrada para gerenciamento de clínicas que permite:

- 🏢 **Gestão Multi-Clínicas**: Administração centralizada de múltiplas unidades
- 📦 **Controle de Estoque**: Gerenciamento completo de materiais e movimentações
- 💰 **Dashboard Financeiro**: Visualização de receitas, despesas e análises
- 👥 **Gestão de Funcionários**: Controle de usuários e permissões por clínica
- 📊 **Relatórios e Análises**: Gráficos interativos com Chart.js

## 🛠️ Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM
- **SQLite** - Banco de dados
- **JWT Bearer** - Autenticação e autorização
- **BCrypt.Net** - Criptografia de senhas
- **Swagger/OpenAPI** - Documentação da API

### Frontend
- **Blazor WebAssembly** - Framework SPA
- **Chart.js Blazor** - Visualização de dados
- **CSS3** - Estilos modernos e responsivos

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas (Clean Architecture):

```
ClinicApp/
├── Api/                    # Camada de API (Controllers, Middleware)
├── Client/                 # Aplicação Blazor WebAssembly
│   ├── Pages/             # Páginas da aplicação
│   ├── Services/          # Serviços do cliente
│   ├── Auth/              # Autenticação
│   └── Layout/            # Layout e componentes de UI
├── Core/                   # Camada de domínio
│   ├── Entities/          # Entidades de negócio
│   └── Enums/             # Enumerações
├── Infrastructure/         # Camada de infraestrutura
│   └── Data/              # DbContext e configurações
└── Shared/                 # DTOs e contratos compartilhados
    └── DTOs/              # Data Transfer Objects
```

## 🚀 Funcionalidades

### 👤 Gestão de Usuários
- Controle de acesso baseado em papéis (Master/User)
- Autenticação JWT
- Associação de usuários a clínicas específicas

### 🏢 Gestão de Clínicas
- Cadastro e edição de clínicas
- Dashboard individual por unidade
- Análise de performance por clínica

### 📦 Controle de Estoque
- **Estoque Geral**: Visão unificada de todos os materiais
- **Estoque por Clínica**: Gestão individualizada
- **Movimentações**: Rastreamento completo de entradas e transferências
- **Categorias**: Organização de materiais por categoria
- **Histórico de Preços**: Evolução de custos ao longo do tempo

### 💵 Gestão Financeira
- Registro de receitas e despesas
- Dashboard com gráficos interativos
- Filtros por período e clínica
- Análise de despesas por categoria
- Resumo financeiro com totais

### 📊 Dashboards
- **Dashboard Principal**: Visão geral do sistema
- **Dashboard Financeiro**: Análises financeiras detalhadas
- Gráficos de pizza, barra e linha
- Filtros dinâmicos por data

## 📦 Instalação

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Opcional) Visual Studio 2022 ou VS Code

### Passos

1. **Clone o repositório**
```bash
git clone https://github.com/JonatasFreire2904/ClinicApp.git
cd ClinicApp
```

2. **Restaure as dependências**
```bash
dotnet restore
```

3. **Execute o projeto**
```bash
cd Api
dotnet run
```

4. **Acesse a aplicação**
- Frontend: `https://localhost:7xxx` (veja o console para a porta exata)
- API: `https://localhost:7xxx/swagger`

## 🗄️ Banco de Dados

O projeto utiliza SQLite com Entity Framework Core. O banco de dados é criado automaticamente na primeira execução.

### Modelos Principais

- **User**: Usuários do sistema
- **Clinic**: Clínicas/Unidades
- **Material**: Materiais/Produtos
- **StockMovement**: Movimentações de estoque
- **ClinicStock**: Estoque por clínica
- **FinancialTransaction**: Transações financeiras

## 🔐 Autenticação

O sistema utiliza autenticação JWT com os seguintes papéis:

- **Master**: Acesso total ao sistema, gestão de todas as clínicas
- **User**: Acesso limitado às clínicas associadas

## 🎨 Interface

A interface foi desenvolvida com foco em:
- ✨ Design moderno e limpo
- 📱 Responsividade
- 🎯 UX intuitiva
- 🌈 Paleta de cores verde moderna
- 🔄 Animações suaves

## 📝 Principais Entidades

### Transações Financeiras
```csharp
- TransactionType: Income (Receita) | Expense (Despesa)
- Associadas a clínicas específicas
- Rastreamento de usuário criador
```

### Movimentações de Estoque
```csharp
- MovementType: Inbound (Entrada) | Transfer (Transferência) | Outbound (Saída)
- Controle de quantidade e preço unitário
- Histórico completo de movimentações
```

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para:

1. Fazer um Fork do projeto
2. Criar uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abrir um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.

## 👨‍💻 Autor

**Jonatas Freire**

- GitHub: [@JonatasFreire2904](https://github.com/JonatasFreire2904)

## 📞 Contato

Para dúvidas, sugestões ou feedback, sinta-se à vontade para abrir uma issue no GitHub.

---

⭐ Se este projeto foi útil para você, considere dar uma estrela!
