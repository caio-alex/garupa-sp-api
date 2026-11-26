# Garupa SP · Proteção do cadastro

O fluxo demonstrado começa no POST /api/v1/usuarios. O cadastro e a emissão de JWT usam os serviços existentes. Após criar o usuário, o controller encaminha o DTO para a fronteira de auditoria. O parser identifica os campos por caminho, inclusive os dois campos de Contato. A marcação é a lista de caminhos sensíveis. O log aceita apenas os nomes marcados, o código de evento e a correlação. O painel informa que o registro dos valores foi impedido.

| Entrada | Classificação | Saída para log, painel e Gemini |
|---|---|---|
| Nome, Telefone | Identificação e contato | Somente nomes dos campos |
| Contato.Nome, Contato.Telefone | Dados do contato de emergência | Somente nomes dos campos |
| Senha | Credencial | Somente a indicação de que o campo foi protegido |

O controle cobre o destino de auditoria deste cadastro. O protótipo guarda os 500 eventos mais recentes em memória para o painel. Reiniciar a API limpa esse histórico. O log estruturado segue a retenção do provedor de logs configurado. O backend mantém as regras e o nível amarelo mesmo sem acesso ao Gemini.

## Executar e demonstrar

1. No Visual Studio, abra **Gerenciar Segredos do Usuário** no projeto Garupa API. Preserve as configurações de banco e JWT e acrescente uma seção Gemini com ApiKey e Modelo. Use o ID de um modelo de texto disponível no projeto do Google AI Studio.
2. Com o banco de desenvolvimento preparado como indicado no README da API, rode dotnet test e inicie a API no perfil Development.
3. Use o Swagger para enviar o exemplo abaixo a POST /api/v1/usuarios. Se o telefone de teste já estiver cadastrado, use outro número fictício aceito pela validação. O retorno esperado é HTTP 201. Copie o token sem incluí-lo nas evidências.

```json
{
  "nome": "Teste Cadastro",
  "telefone": "11900001001",
  "contato": { "nome": "Teste Contato", "telefone": "11900001002" },
  "senha": "SegredoTeste#47"
}
```

4. Na mesma origem da API, abra /painel-seguranca e consulte com o token emitido. O painel pede GET /api/v1/seguranca/meu-evento. A API determina o titular pelo JWT. A página exibe a ação, o risco, a origem da mensagem e a correlação.
5. Localize PII_LOG_BLOCKED no console e compare a correlação com a do painel. Verifique que os nomes, números e senha de teste não aparecem. Registre a evidência do teste AuditoriaCadastroTests, que também verifica o corpo enviado ao provedor simulado.
6. Confirme que a origem no painel é Gemini para a evidência da chamada real. Se for Mensagem local, revise a chave, o modelo e a cota. Um retorno sem token deve ser HTTP 401. Para testar a contingência, remova temporariamente a configuração do modelo e reinicie. Faça outro cadastro, pois o histórico foi reiniciado.
7. No vídeo mostre a entrada fictícia, o log protegido e o painel. No PDF anexe o mapa de campos, trechos do parser e do logger, o resultado dos testes, o painel e o link do vídeo. Entregue o PDF separado do código. Oculte tokens, chaves e a resposta de autenticação nas capturas.
