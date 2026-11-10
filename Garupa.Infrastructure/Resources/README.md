# Dados geográficos e catálogo

Os arquivos geográficos foram obtidos em 09/11/2026 das APIs públicas do IBGE. O município tem código 3550308.

- A lista de 96 distritos preserva os identificadores do IBGE. [API de localidades](https://servicodados.ibge.gov.br/api/v1/localidades/municipios/3550308/distritos)
- O GeoJSON do limite municipal foi solicitado com a qualidade máxima oferecida pelo serviço. A API de relatos faz teste de ponto no polígono, incluindo os anéis internos. [API de malhas](https://servicodados.ibge.gov.br/api/v3/malhas/municipios/3550308?formato=application/vnd.geo%2Bjson&qualidade=maxima)
- Os textos do catálogo são adaptações curtas de orientações do DNIT e da Secretaria de Transportes de Vitória. Cada registro mantém a URL de origem. [Orientações do DNIT](https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada) · [Dicas para motociclistas de Vitória](https://vitoria.es.gov.br/setran/dicas-para-motociclistas)

Em 10/11/2026, a revisão técnica conferiu limite de 140 caracteres, tom informativo, fontes públicas e cobertura dos cinco contextos (`geral`, `chuva`, `noite`, `domingo`, `ponto_critico`). As duas mensagens acrescentadas abordam visibilidade à noite e inspeção antes da saída no domingo. A revisão técnica automatizada e documental deve ser complementada pela avaliação de utilidade da equipe com os usuários do produto.

A tabela de risco deve ser produzida pelo modelo e importada pelo comando `carregar-risco`. Os recursos distribuídos com a API incluem dados geográficos de referência e mensagens. Os testes criam suas próprias células fictícias.

O importador aceita os IDs do IBGE. Para um CSV com IDs GeoSampa, ele encontra o distrito pelo nome oficial, ignorando acentos e pontuação, e converte para o ID IBGE. Nomes desconhecidos são recusados antes de alterar a tabela.
