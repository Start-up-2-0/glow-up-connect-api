using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estabelecimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estabelecimentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Planos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Periodo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LimiteProfissionais = table.Column<int>(type: "integer", nullable: true),
                    LimiteServicos = table.Column<int>(type: "integer", nullable: true),
                    LimiteAgendamentos = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Senha = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookPagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Processado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErroProcessamento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookPagamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HorariosFuncionamentoEstabelecimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamentoEstabelecimento", x => x.Id);
                    table.CheckConstraint("CK_HorariosFuncionamentoEstabelecimento_Horario", "\"HoraInicio\" < \"HoraFim\"");
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamentoEstabelecimento_Estabelecimentos_Estab~",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstabelecimentoUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    RoleNoEstabelecimento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstabelecimentoUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstabelecimentoUsuarios_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstabelecimentoUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    NomePublico = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Biografia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TipoProfissional = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profissionais_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Agendamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioClienteId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                    table.CheckConstraint("CK_Agendamentos_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Agendamentos_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Usuarios_UsuarioClienteId",
                        column: x => x.UsuarioClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanoId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenovacaoAutomatica = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewaySubscriptionId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GatewayCustomerId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UltimoPagamentoId = table.Column<int>(type: "integer", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.CheckConstraint("CK_Assinaturas_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Assinaturas_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "Planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    SaldoTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SaldoDisponivel = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SaldoRetido = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                    table.CheckConstraint("CK_Caixas_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Caixas_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enderecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    Cep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enderecos", x => x.Id);
                    table.CheckConstraint("CK_Enderecos_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Enderecos_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enderecos_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosAtendimentoProfissional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosAtendimentoProfissional", x => x.Id);
                    table.CheckConstraint("CK_HorariosAtendimentoProfissional_Horario", "\"HoraInicio\" < \"HoraFim\"");
                    table.ForeignKey(
                        name: "FK_HorariosAtendimentoProfissional_Estabelecimentos_Estabeleci~",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HorariosAtendimentoProfissional_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfissionalEstabelecimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PodeReceberAgendamento = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionalEstabelecimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionalEstabelecimentos_Estabelecimentos_Estabelecimen~",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfissionalEstabelecimentos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Servicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrecoBase = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicos", x => x.Id);
                    table.CheckConstraint("CK_Servicos_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Servicos_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicos_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: true),
                    AssinaturaId = table.Column<int>(type: "integer", nullable: true),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayPaymentId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MetodoPagamento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PagoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.CheckConstraint("CK_Pagamentos_Origem", "((\"AgendamentoId\" IS NOT NULL AND \"AssinaturaId\" IS NULL) OR (\"AgendamentoId\" IS NULL AND \"AssinaturaId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Pagamentos_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComissoesProfissional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalEstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoComissao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ValorFixo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FimVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComissoesProfissional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComissoesProfissional_ProfissionalEstabelecimentos_Profissi~",
                        column: x => x.ProfissionalEstabelecimentoId,
                        principalTable: "ProfissionalEstabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetasProfissional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalEstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoMeta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantidadeAtendimentos = table.Column<int>(type: "integer", nullable: true),
                    ValorFaturamento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    InicioPeriodo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FimPeriodo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasProfissional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasProfissional_ProfissionalEstabelecimentos_Profissional~",
                        column: x => x.ProfissionalEstabelecimentoId,
                        principalTable: "ProfissionalEstabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgendamentoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: false),
                    ServicoId = table.Column<int>(type: "integer", nullable: false),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RepassadoDeProfissionalId = table.Column<int>(type: "integer", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentoItens", x => x.Id);
                    table.CheckConstraint("CK_AgendamentoItens_Horario", "\"Inicio\" < \"Fim\"");
                    table.ForeignKey(
                        name: "FK_AgendamentoItens_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgendamentoItens_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgendamentoItens_Profissionais_RepassadoDeProfissionalId",
                        column: x => x.RepassadoDeProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgendamentoItens_Servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfissionalServicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: false),
                    ServicoId = table.Column<int>(type: "integer", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionalServicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionalServicos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfissionalServicos_Servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LancamentosCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaixaId = table.Column<int>(type: "integer", nullable: false),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: true),
                    PagamentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentosCaixa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LancamentosCaixa_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LancamentosCaixa_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LancamentosCaixa_Pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "Pagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LancamentosCaixa_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoItens_AgendamentoId",
                table: "AgendamentoItens",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoItens_ProfissionalId",
                table: "AgendamentoItens",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoItens_RepassadoDeProfissionalId",
                table: "AgendamentoItens",
                column: "RepassadoDeProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoItens_ServicoId",
                table: "AgendamentoItens",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_EstabelecimentoId",
                table: "Agendamentos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_ProfissionalAutonomoId",
                table: "Agendamentos",
                column: "ProfissionalAutonomoId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_UsuarioClienteId",
                table: "Agendamentos",
                column: "UsuarioClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_EstabelecimentoId",
                table: "Assinaturas",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_PlanoId",
                table: "Assinaturas",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_ProfissionalAutonomoId",
                table: "Assinaturas",
                column: "ProfissionalAutonomoId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_EstabelecimentoId",
                table: "Caixas",
                column: "EstabelecimentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_ProfissionalAutonomoId",
                table: "Caixas",
                column: "ProfissionalAutonomoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesProfissional_ProfissionalEstabelecimentoId",
                table: "ComissoesProfissional",
                column: "ProfissionalEstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Enderecos_EstabelecimentoId",
                table: "Enderecos",
                column: "EstabelecimentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enderecos_ProfissionalAutonomoId",
                table: "Enderecos",
                column: "ProfissionalAutonomoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_PublicGuid",
                table: "Estabelecimentos",
                column: "PublicGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentoUsuarios_EstabelecimentoId_UsuarioId",
                table: "EstabelecimentoUsuarios",
                columns: new[] { "EstabelecimentoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentoUsuarios_UsuarioId",
                table: "EstabelecimentoUsuarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosAtendimentoProfissional_EstabelecimentoId",
                table: "HorariosAtendimentoProfissional",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosAtendimentoProfissional_ProfissionalId_Estabelecime~",
                table: "HorariosAtendimentoProfissional",
                columns: new[] { "ProfissionalId", "EstabelecimentoId", "DiaSemana", "HoraInicio", "HoraFim" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosFuncionamentoEstabelecimento_EstabelecimentoId_DiaS~",
                table: "HorariosFuncionamentoEstabelecimento",
                columns: new[] { "EstabelecimentoId", "DiaSemana", "HoraInicio", "HoraFim" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_AgendamentoId",
                table: "LancamentosCaixa",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_CaixaId",
                table: "LancamentosCaixa",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_PagamentoId",
                table: "LancamentosCaixa",
                column: "PagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_ProfissionalId",
                table: "LancamentosCaixa",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_MetasProfissional_ProfissionalEstabelecimentoId",
                table: "MetasProfissional",
                column: "ProfissionalEstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MetasProfissional_Status",
                table: "MetasProfissional",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_AgendamentoId",
                table: "Pagamentos",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_AssinaturaId",
                table: "Pagamentos",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_GatewayPaymentId",
                table: "Pagamentos",
                column: "GatewayPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Planos_Nome",
                table: "Planos",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_PublicGuid",
                table: "Profissionais",
                column: "PublicGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalEstabelecimentos_EstabelecimentoId",
                table: "ProfissionalEstabelecimentos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalEstabelecimentos_ProfissionalId",
                table: "ProfissionalEstabelecimentos",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalServicos_ProfissionalId_ServicoId",
                table: "ProfissionalServicos",
                columns: new[] { "ProfissionalId", "ServicoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalServicos_ServicoId",
                table: "ProfissionalServicos",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_EstabelecimentoId",
                table: "Servicos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_ProfissionalAutonomoId",
                table: "Servicos",
                column: "ProfissionalAutonomoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookPagamentos_Gateway_EventId",
                table: "WebhookPagamentos",
                columns: new[] { "Gateway", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentoItens");

            migrationBuilder.DropTable(
                name: "ComissoesProfissional");

            migrationBuilder.DropTable(
                name: "Enderecos");

            migrationBuilder.DropTable(
                name: "EstabelecimentoUsuarios");

            migrationBuilder.DropTable(
                name: "HorariosAtendimentoProfissional");

            migrationBuilder.DropTable(
                name: "HorariosFuncionamentoEstabelecimento");

            migrationBuilder.DropTable(
                name: "LancamentosCaixa");

            migrationBuilder.DropTable(
                name: "MetasProfissional");

            migrationBuilder.DropTable(
                name: "ProfissionalServicos");

            migrationBuilder.DropTable(
                name: "WebhookPagamentos");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "ProfissionalEstabelecimentos");

            migrationBuilder.DropTable(
                name: "Servicos");

            migrationBuilder.DropTable(
                name: "Agendamentos");

            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "Estabelecimentos");

            migrationBuilder.DropTable(
                name: "Planos");

            migrationBuilder.DropTable(
                name: "Profissionais");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
