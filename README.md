# Garupa SP API

API em .NET 9 com camadas Domain, Application, Infrastructure e API. PostgreSQL é o banco da aplicação. Os testes usam SQLite em memória e WebApplicationFactory.

## Executar

Instale o SDK .NET 9, PostgreSQL e a ferramenta `dotnet-ef` da versão 9. Configure a conexão por variável de ambiente no PowerShell usando um banco local de desenvolvimento.

```powershell
$env:ConnectionStrings__GarupaDb = 'Host=localhost;Database=garupa;Username=SEU_USUARIO;Password=SUA_SENHA'
dotnet restore 'Garupa API.sln'
dotnet run --project 'Garupa API' -- preparar-banco
dotnet run --project 'Garupa API'
```

O comando `preparar-banco` aplica as migrations e converte fichas e tokens antigos para armazenamento protegido. Execute-o antes de iniciar a API após atualizar o projeto. A conversão pode ser repetida e mantém os links de acompanhamento existentes.

Em desenvolvimento, a chave AES de 256 bits é criada em `.local/keys/ficha-medica.key`. Faça backup desse arquivo junto com o banco. Nos demais ambientes, configure `Criptografia__Chave` em Base64 ou `Criptografia__ArquivoChave` apontando para uma chave persistente de 32 bytes. A API exige uma chave existente nesses ambientes. Trocar ou perder a chave impede a leitura dos dados protegidos.

As migrations de proteção de tokens usam evolução para a frente. Para retornar ao formato anterior, restaure um backup compatível. O downgrade automático foi bloqueado para evitar truncar tokens protegidos ou reconstruir coordenadas já removidas.

### PostgreSQL local no Windows

O script abaixo usa os binários já instalados do PostgreSQL 17 e a conexão dos User Secrets ou de `ConnectionStrings__GarupaDb`. Ele inicializa uma instância exclusiva do projeto em `127.0.0.1:5433`, com banco `garupa_sp`, autenticação por senha e dados em `.local/postgres`, ignorados pelo Git. A instância é iniciada em segundo plano e pode ser retomada pelo mesmo comando.

```powershell
.\scripts\Start-LocalDatabase.ps1
```

Informe `-PostgresBin` se os executáveis estiverem em outro diretório. O script exige usuário e senha já configurados e aceita apenas o endereço local, a porta 5433 e o banco `garupa_sp`. O arquivo temporário necessário para fornecer a senha ao `initdb` é removido ao concluir a inicialização.

Para parar essa instância após o desenvolvimento, execute na raiz do projeto.

```powershell
& 'C:\Program Files\PostgreSQL\17\bin\pg_ctl.exe' -D "$PWD\.local\postgres" -w -m fast stop
```

O Swagger está em `/swagger` no ambiente Development. A URL local aparece no terminal. O arquivo `Garupa API/Garupa API.http` contém exemplos do fluxo.

## Cadastro e SOS

As rotas da API usam `/api/v1`. O cadastro exige um objeto `contato` com nome e telefone. Usuário e contato inicial são gravados na mesma transação do EF Core. Telefones brasileiros aceitam DDD, código +55 e formatação, e são armazenados apenas com dígitos nacionais.

```json
{
  "nome": "Pessoa fictícia",
  "telefone": "11987654321",
  "contato": { "nome": "Contato fictício", "telefone": "1134567890" }
}
```

| Método | Rota | Resposta |
| --- | --- | --- |
| POST | `/api/v1/usuarios` | 201 com `usuarioId` |
| GET | `/api/v1/usuarios/{id}` | 200 ou 404 |
| POST | `/api/v1/usuarios/{id}/contatos` | 201 ou 404 |
| POST | `/api/v1/sos` | 201 com `sosId` e `tokenAcompanhamento` |
| GET | `/api/v1/acompanhar/{token}` | 200 ou 404 |
| PATCH | `/api/v1/sos/{id}/cancelar` | 204, 404 ou 409 |
| PATCH | `/api/v1/sos/{id}/encerrar` | 204, 404 ou 409 |
| GET | `/api/v1/saude` | 200 com banco conectado ou 503 |

`/saude` também atende o monitor existente. As antigas rotas sem prefixo de SOS e vestível foram movidas para `/api/v1`.

O SOS exige GUID não vazio em `eventoId`, usuário existente, `tipo` (`queda`, `mal_estar`, `agressao_assalto`), `origem` (`celular`, `vestivel`), horário ISO 8601 UTC e coordenadas válidas. Horários futuros são recusados. Um reenvio com o mesmo evento e os mesmos dados devolve o mesmo ID e token. Reutilizar o evento com outros dados retorna 409. O índice único e o tratamento de conflito no PostgreSQL também protegem envios concorrentes. A busca do token usa SHA-256. A cópia necessária para devolver o token em um reenvio fica criptografada com AES-GCM.

O acompanhamento retorna posição, horário e estado. Cancelar e encerrar são idempotentes. Uma tentativa de trocar um estado terminal por outro retorna 409. Conflitos de atualização concorrente retornam 409 para o cliente consultar o estado atual e repetir a operação.

Foram adotadas as propostas de encerrar o SOS 12 horas após seu recebimento na API e expirar o token 12 horas após cancelamento ou encerramento. O prazo considera o instante efetivo do encerramento, mesmo se a consulta ocorrer mais tarde. Repetir uma transição preserva esse prazo. Um serviço C# executa a manutenção a cada minuto e ao iniciar a aplicação. Ele encerra SOS antigos, apaga coordenadas de SOS após 30 dias, descarta alertas expirados e remove eventos do vestível consumidos há mais de um dia. O acompanhamento também calcula o encerramento na consulta, sem esperar o próximo ciclo.

Erros usam ProblemDetails com mensagens genéricas. Os logs JSON registram método, template da rota, status, duração e correlação, sem corpo, query string ou valores da rota. A resposta inclui `X-Correlation-ID`. O log padrão de exceções e comandos SQL que poderia expor parâmetros é filtrado. Use apenas dados pessoais fictícios no demo.

## Testes

```powershell
dotnet test 'Garupa API.sln'
```

Os testes cobrem os endpoints principais, persistência, validação, criptografia, migração de dados antigos, reenvio, conflitos de estado, privacidade dos logs, retenção e expiração usando relógio controlado. A execução padrão usa SQLite e dispensa o PostgreSQL. Os cinco testes exclusivos de concorrência do PostgreSQL são ignorados nessa execução.

Para executar a suíte com PostgreSQL, inicie o banco local e execute os comandos abaixo. O script usa a conexão dos User Secrets ou de `ConnectionStrings__GarupaDb`. A compilação fica em `.local/postgres-tests`, permitindo testar enquanto a API está aberta no Visual Studio.

```powershell
dotnet restore 'Garupa API.sln' --artifacts-path '.local/postgres-tests'
.\scripts\Test-Postgres.ps1
# Inclui relatório Cobertura em .local/coverage-postgres
.\scripts\Test-Postgres.ps1 -Coverage
```

Cada instância de teste cria um schema `garupa_tests_` com sufixo aleatório, aplica as migrations nesse schema e o remove ao terminar. A suíte verifica reenvios HTTP simultâneos, recuperação de conflito pelo índice único, consumo único do vestível e proteção contra transições ou exclusões concorrentes. O schema da aplicação permanece separado dos dados de teste.

Na validação local de 09/11/2026, os 50 testes passaram com PostgreSQL. O relatório Cobertura mediu 92,72% de cobertura de linhas no domínio e 99,53% na camada Application. A verificação da API em execução confirmou 28 operações no Swagger e o fluxo HTTP com cadastro, consentimentos, ficha, perfil, conteúdo, SOS, vestível e relato. O usuário fictício dessa verificação foi excluído ao final.

## Demais recursos da API

Todas as rotas abaixo usam o prefixo `/api/v1`.

| Método | Rota | Comportamento |
| --- | --- | --- |
| PUT | `/usuarios/{id}` | Edita nome e telefone |
| PUT / DELETE | `/usuarios/{id}/contatos/{contatoId}` | Edita ou remove contato, preservando pelo menos um |
| PUT | `/usuarios/{id}/consentimentos` | Atualiza os cinco consentimentos explícitos |
| PUT | `/usuarios/{id}/ficha-medica` | Cria ou edita ficha opcional com criptografia no banco |
| PUT | `/usuarios/{id}/perfil` | Atualiza usos, horários habituais e até cinco distritos |
| DELETE | `/usuarios/{id}` | Exclui usuário e todos os dados dependentes |
| GET | `/distritos` | Lista os 96 distritos com IDs IBGE |
| GET | `/mensagens` | Filtra mensagens ativas por contexto e tipo de uso |
| GET | `/risco` | Filtra células por posição, raio, faixa, tipo de dia e chuva |
| GET | `/risco/celulas/{id}` | Mostra as faixas e contextos de uma célula |
| GET | `/plano-do-turno` | Pontos dos distritos do perfil e até duas mensagens do contexto |
| POST / DELETE | `/usuarios/{id}/vestivel` | Pareia ou desfaz o pareamento |
| POST | `/eventos-vestivel` | Recebe impacto, SOS ou cancelamento de dispositivo pareado |
| GET | `/eventos-vestivel/pendentes` | Consome eventos uma única vez por usuário |
| POST | `/usuarios/{id}/alertas-vestivel` | Enfileira alerta para o dispositivo pareado |
| GET | `/vestivel/{dispositivoId}/alertas/pendentes` | Consome alertas uma vez, com expiração em 30 segundos |
| POST | `/relatos` | Cria relato idempotente e valida o limite municipal |

Os consentimentos são `localizacaoEmUso`, `localizacaoEmSegundoPlano`, `fichaMedica`, `historicoDeLocais` e `perfilDeUso`. Todos devem aparecer no PUT. Localização em segundo plano exige localização em uso. Revogar ficha ou perfil remove seus dados na mesma transação. O histórico aprendido de locais continua sob responsabilidade do aparelho. A API recebe apenas os distritos declarados.

Ficha e perfil exigem seus respectivos consentimentos, retornando 409 enquanto não forem concedidos. Os campos da ficha são opcionais. Ela permanece fora da consulta de usuário e do acompanhamento público. A tela da testemunha usa os dados mantidos pelo aplicativo conforme o contrato.

O perfil aceita `entregador_app`, `trabalho_moto`, `ida_volta` e `lazer`. As faixas horárias vão de 0 a 5, com quatro horas cada. Os tipos de dia são `util`, `sabado` e `domingo`. As consultas de risco e plano usam percentil mínimo 95 e ordenam por score. O score da célula é preservado entre perfis. Cada resposta limita a lista a 500 pontos. O raio aceito vai de 1 a 50.000 metros.

O plano funciona sem perfil, retornando mensagens gerais e uma lista vazia de pontos personalizados. Com perfil, seleciona as células dos distritos declarados e mensagens do contexto da consulta e dos tipos de uso. Os horários habituais são armazenados para o cadastro. O contexto atual do plano é determinado pelos parâmetros enviados na consulta.

O vestível recebe `turno_ativo`, `ponto_critico`, `mensagem`, `contagem_queda`, `sos_enviado`, `cancelado` e `turno_encerrado`. A expiração usa o horário do evento em UTC. Cada consulta consome até 100 registros, em ordem de ID. Desparear ou trocar o dispositivo elimina as pendências anteriores. Dispositivo sem pareamento recebe 404.

## Carga do risco

Depois de preparar o banco, importe o CSV produzido pelo modelo.

```powershell
dotnet run --project 'Garupa API' -- carregar-risco 'CAMINHO\risco_celula.csv'
```

O importador valida todo o arquivo antes de substituir o snapshot em uma transação. Aceita UTF-8, vírgula ou ponto e vírgula como separador, campos entre aspas e números com ponto ou vírgula decimal. Recusa contextos inválidos, distritos desconhecidos, duplicatas e arquivos sem células. O limite é de 500.000 linhas.

As colunas obrigatórias são `celula_id`, `centro_lat`, `centro_lon`, `faixa_horaria`, `tipo_dia`, `chuva` (0 ou 1), `score`, `percentil`, `via_referencia`, `distrito_id`, `distrito_nome` e `versao_modelo`. Para IDs GeoSampa, a conversão usa o nome oficial do distrito. A lista retornada pela API sempre usa IDs IBGE.

As consultas retornam listas vazias enquanto o CSV do modelo não for importado. Os dados geográficos e as fontes do catálogo estão documentados em `Garupa.Infrastructure/Resources/README.md`.
