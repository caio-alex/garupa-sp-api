namespace Garupa.Api.Documentacao;

public static class PainelSeguranca
{
    public const string Html = """
<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Privacidade dos seus dados | Garupa SP</title>
<style>body{font:1rem/1.6 system-ui;max-width:44rem;margin:8vh auto;padding:1rem;color:#182d32}label,input,button{display:block}input,button{padding:.6rem;font:inherit}input{width:85%}button{margin:1rem 0;background:#164841;color:white;border:0}section{padding:1rem;background:#fff5d8;border-left:4px solid #896300}</style>
<h1>Privacidade dos seus dados</h1><form id="form"><label for="token">Token de acesso do usuário de teste</label>
<input id="token" type="password" autocomplete="off" required><button>Consultar meu evento</button></form>
<p role="status" id="status"></p><section hidden id="card"><strong id="risco"></strong><p id="mensagem"></p><small id="detalhes"></small></section>
<script>
document.querySelector('form').onsubmit = async e => {
 e.preventDefault();
 const el = id => document.getElementById(id);
 el('card').hidden = true;
 el('status').textContent = 'Consultando…';
 const token = el('token').value.trim();
 el('token').value = '';
 try {
   const r = await fetch('/api/v1/seguranca/meu-evento',{headers:{Authorization:'Bearer '+token},cache:'no-store'});
   if(r.status===204){el('status').textContent='Nenhum evento recente para este usuário.';return;}
   if(!r.ok){el('status').textContent='Consulta não concluída (HTTP '+r.status+').';return;}
   const d = await r.json();
   el('risco').textContent=d.risco+' · '+d.acao;
   el('mensagem').textContent=d.mensagem;
   el('detalhes').textContent=[d.origem,new Date(d.data).toLocaleString('pt-BR'),d.campos.join(', '),'Correlação '+d.correlacao].join(' | ');
   el('status').textContent='Último evento de proteção dos seus dados';
   el('card').hidden=false;
 } catch {el('status').textContent='A API está indisponível.';}
};
</script></html>
""";
}
