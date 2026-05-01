# 📌 Glow Up Connect — Contexto do Projeto

## 🧠 Visão Geral

O **Glow Up Connect** é uma plataforma de agendamentos e serviços voltada para profissionais independentes e estabelecimentos (como barbearias, salões e clínicas de estética), funcionando como um **marketplace com gestão completa e intermediação de pagamentos**.

A plataforma conecta:

- Clientes (quem agenda)
- Profissionais (quem executa)
- Estabelecimentos (com múltiplos profissionais)

---

## 🎯 Objetivo

Criar um ecossistema onde:

- Clientes encontrem profissionais próximos
- Agendem serviços facilmente
- Realizem pagamentos pela plataforma
- Profissionais recebam com segurança
- A plataforma monetize com taxas ou assinatura

---

## 👥 Tipos de Usuário

### Cliente

- Buscar profissionais
- Visualizar serviços
- Agendar horários
- Realizar pagamentos

### Profissional

- Gerenciar agenda
- Cadastrar serviços
- Receber pagamentos
- Solicitar saque

### Estabelecimento

- Gerenciar múltiplos profissionais
- Controlar agenda coletiva
- Definir comissões

---

## 💰 Modelo de Monetização

### Plano Gratuito

- Taxa fixa por agendamento (ex: R$5)
- Valor retido na plataforma
- Saque mínimo (ex: R$100)

### Plano Assinatura

- Sem taxa por agendamento
- Saques liberados normalmente (com regras mínimas)

---

## 💳 Fluxo de Pagamento

1. Cliente paga pela plataforma
2. Valor vai para conta da plataforma
3. Sistema:
   - desconta taxa (se plano gratuito)
   - registra saldo do profissional
4. Profissional solicita saque
5. Plataforma valida regras e libera

---

## 🔐 Regras de Segurança

- Saque mínimo obrigatório
- Validação de saldo disponível
- Bloqueio de saque em caso de:
  - transações pendentes
  - risco de chargeback
- Controle de:
  - saldo total
  - saldo liberado
  - saldo em retenção

---

## 📍 Funcionalidades

### Agendamento

- Escolha de data e horário
- Baseado na disponibilidade do profissional
- Evita conflitos

### Geolocalização

- Exibe profissionais próximos
- Baseado na localização do cliente

### Serviços

- Nome
- Preço
- Descrição
- Tempo de execução

### Agenda

- Dias trabalhados
- Horários disponíveis
- Bloqueios personalizados

---

## 🧠 Regras de Negócio

- Profissional pode ser:

  - Independente
  - Vinculado a um estabelecimento
- Estabelecimento pode:

  - Definir comissão
  - Gerenciar agenda de equipe
- Cada agendamento:

  - Gera uma transação financeira
  - Deve ser rastreável

---

## ⚙️ Stack Tecnológica

### Backend

- .NET (API)

### Frontend

- Vue 3 + Vite + TypeScript
- Tailwind CSS + Flowbite

### Banco de Dados

- PostgreSQL

### Integrações

- Gateways de pagamento (ex: Mercado Pago, Pargame)

---

## 📊 Entidades Principais

- Users
- Professionals
- Establishments
- Services
- Appointments
- Transactions
- Withdrawals

---

## 🔄 Fluxo Principal

1. Cliente acessa a plataforma
2. Busca profissional
3. Agenda serviço
4. Realiza pagamento
5. Plataforma registra transação
6. Serviço é executado
7. Valor fica disponível
8. Profissional solicita saque

---

## 🚀 Diferenciais

- Marketplace + gestão + financeiro integrados
- Modelo híbrido de monetização
- Controle financeiro seguro
- Foco em profissionais independentes e pequenos negócios

