# Validação da API C# em .NET 10

Verificação local concluída em 10/11/2026.

## Versões

| Componente | Versão |
| --- | --- |
| SDK .NET | 10.0.404 |
| Runtime | 10.0.15 LTS |
| EF Core e dotnet-ef local | 10.0.15 |
| Provider PostgreSQL | 10.0.3 |
| Swagger Swashbuckle | 10.3.1 |

O SDK está fixado em `global.json` e a ferramenta de migrations está fixada em `dotnet-tools.json`. As versões dos pacotes estão explícitas nos projetos.

## Testes e cobertura

Foram aprovados 58 testes funcionais, de integração, segurança e contratos, seguidos de um teste de desempenho. A cobertura foi coletada na execução funcional. O benchmark rodou em outro processo, sem o custo da instrumentação de cobertura.

| Camada | Cobertura de linhas |
| --- | --- |
| Domain | 92,99% |
| Application | 99,17% |

Os cenários adicionais conferem prioridade da faixa explícita, seleção do horário habitual, fallback para o horário atual de São Paulo, os cinco contextos das mensagens, o tamanho das respostas com textos Unicode longos e os exemplos e códigos HTTP do Swagger.

[Log da execução](evidencias/testes-net10.log) · [Resumo da cobertura](evidencias/cobertura-resumo.json)

## Desempenho

A medição usa Kestrel HTTP em loopback e PostgreSQL local. A carga tem 10.000 células fictícias, com 500 pontos retornados por consulta. São cinco aquecimentos e cem requisições por rota, em lotes de quatro chamadas simultâneas. O schema é exclusivo do teste e é removido ao final. A fixture usa conexões sem pooling e desativa a manutenção agendada no ambiente Testing.

| Consulta | p50 | p95 | Máximo | Maior resposta |
| --- | --- | --- | --- | --- |
| Risco | 260,45 ms | 342,21 ms | 384,58 ms | 156.701 bytes |
| Plano do turno | 308,67 ms | 434,60 ms | 468,02 ms | 156.973 bytes |

As duas consultas atenderam às metas propostas de p95 até 500 ms e até 200.000 bytes nessa carga local. Esses resultados caracterizam o cenário sintético descrito. A validação com o CSV real permanece dependente do arquivo do modelo.

Para preservar o limite de tamanho, a lista de pontos tem orçamento de 190.000 bytes, com espaço para mensagens e metadados. A ordenação por score é mantida quando a lista atinge esse orçamento.

[Medições em JSON](evidencias/desempenho-postgres.json)

## Contrato e verificação HTTP

O documento OpenAPI foi obtido por HTTP da API em execução. Ele contém 28 operações, schemas de sucesso e erro, exemplos fictícios dos corpos e parâmetros, além da descrição da faixa horária opcional no plano.

A verificação HTTP confirmou saúde do banco, cadastro, consentimento, edição do perfil, seleção de horário habitual, prioridade da faixa explícita e mensagens noturnas. O usuário fictício foi excluído ao final.

[OpenAPI exportado da aplicação](evidencias/swagger-v1.json)

## Regras adotadas

- A faixa explícita da consulta prevalece sobre os hábitos do perfil.
- Quando a faixa é omitida, o plano seleciona o próximo horário habitual do tipo de dia, em ordem circular de 0 a 5.
- Quando não há hábito aplicável, usa a faixa atual no fuso `America/Sao_Paulo`.
- O score do modelo permanece igual entre perfis.
- O catálogo cobre geral, chuva, noite, domingo e ponto crítico, com fontes oficiais e texto de até 140 caracteres.

As fontes e a revisão técnica das mensagens estão no [documento dos recursos](../Garupa.Infrastructure/Resources/README.md).

## Reprodução

Execute os comandos na raiz do projeto com o PostgreSQL local disponível.

```powershell
.\scripts\Use-DotNet.ps1
dotnet tool restore
dotnet restore 'Garupa API.sln' --artifacts-path '.local/postgres-tests'
.\scripts\Start-LocalDatabase.ps1
.\scripts\Test-Postgres.ps1 -Coverage
```

O relatório atualizado do benchmark fica em `.local/evidencias/desempenho-postgres.json`. Os dados de teste usam schemas temporários. A migration `CompleteCatalogForNet10` acrescenta as mensagens noturna e de domingo. A verificação do EF Core confirmou que o modelo está consistente com as migrations.
