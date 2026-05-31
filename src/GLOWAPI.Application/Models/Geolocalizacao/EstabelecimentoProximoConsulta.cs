using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Models.Geolocalizacao;

public record EstabelecimentoProximoConsulta(Estabelecimento Estabelecimento, double DistanciaKm);
