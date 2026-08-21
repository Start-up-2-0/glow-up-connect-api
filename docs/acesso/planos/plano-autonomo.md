# Planos para profissional autônomo

Variante comercial do mesmo Essencial/Premium, resolvida por `TipoAssinatura.ProfissionalAutonomo`.

## Essencial (autônomo)

- Descrição: *Agenda, clientes e perfil público para quem atende sozinho*
- Preço: **R$ 49,99**/mês
- Agenda, serviços, horários, notificações e e-mail
- Clientes e histórico de atendimentos
- Perfil público e presença no Explorar
- Sem equipe, WhatsApp, caixa ou comissões

## Premium (autônomo)

- Descrição: *WhatsApp, caixa pessoal, financeiro e prioridade no marketplace*
- Preço: **R$ 79,99**/mês
- Tudo do Essencial, mais:
- WhatsApp automático
- Caixa e financeiro pessoal
- Relatórios / dashboard avançado
- Prioridade no marketplace
- Sem equipe, comissões ou multi-loja

## Identificação

`Assinatura.TipoAssinatura = ProfissionalAutonomo` + nome do plano contendo `Essencial` ou `Premium` em `PlanoComercialCatalogo`.
Preço efetivo: `PlanoComercialCatalogo.ResolverPreco(plano, ProfissionalAutonomo)`.
Descrição efetiva: `PlanoComercialCatalogo.ResolverDescricao(plano, ProfissionalAutonomo)`.
