# Dados geográficos e catálogo

Os arquivos geográficos foram obtidos em 09/11/2026 das APIs públicas do IBGE. O município tem código 3550308.

- A lista de 96 distritos preserva os identificadores do IBGE. [API de localidades](https://servicodados.ibge.gov.br/api/v1/localidades/municipios/3550308/distritos)
- O GeoJSON do limite municipal foi solicitado com a qualidade máxima oferecida pelo serviço. A API de relatos faz teste de ponto no polígono, incluindo os anéis internos. [API de malhas](https://servicodados.ibge.gov.br/api/v3/malhas/municipios/3550308?formato=application/vnd.geo%2Bjson&qualidade=maxima)
- Os textos do catálogo são adaptações curtas de orientações do DNIT. A equipe deve revisar o conteúdo para a demonstração. Cada registro mantém a URL de origem. [Orientações do DNIT](https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada)

A tabela de risco deve ser produzida pelo modelo e importada pelo comando `carregar-risco`. Os recursos distribuídos com a API incluem dados geográficos de referência e mensagens. Os testes criam suas próprias células fictícias.

O importador aceita os IDs do IBGE. Para um CSV com IDs GeoSampa, ele encontra o distrito pelo nome oficial, ignorando acentos e pontuação, e converte para o ID IBGE. Nomes desconhecidos são recusados antes de alterar a tabela.
