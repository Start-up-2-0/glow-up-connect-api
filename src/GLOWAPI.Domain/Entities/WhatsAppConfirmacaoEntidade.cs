namespace GLOWAPI.Domain.Entities;

public static class WhatsAppConfirmacaoEntidade
{
    public static void ResetarAoAlterarTelefone(
        string telefoneAnterior,
        string telefoneNovo,
        Action resetConfirmacao)
    {
        if (string.Equals(telefoneAnterior, telefoneNovo, StringComparison.Ordinal))
        {
            return;
        }

        resetConfirmacao();
    }
}
