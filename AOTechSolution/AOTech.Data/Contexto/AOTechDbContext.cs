using System;
using System.Collections.Generic;
using AOTech.Data.Modelos;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Data.Contexto;

public partial class AOTechDbContext : DbContext
{
    public AOTechDbContext()
    {
    }

    public AOTechDbContext(DbContextOptions<AOTechDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CatalogoServicio> CatalogoServicios { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ComprasVideo> ComprasVideos { get; set; }

    public virtual DbSet<Curso> Cursos { get; set; }

    public virtual DbSet<DetalleOrdenServicio> DetalleOrdenServicios { get; set; }

    public virtual DbSet<Encuesta> Encuestas { get; set; }

    public virtual DbSet<FechasBloqueada> FechasBloqueadas { get; set; }

    public virtual DbSet<Membresia> Membresias { get; set; }

    public virtual DbSet<MovimientosInventario> MovimientosInventarios { get; set; }

    public virtual DbSet<OrdenesServicio> OrdenesServicios { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<PagosMembresium> PagosMembresia { get; set; }

    public virtual DbSet<ReglasAgendum> ReglasAgenda { get; set; }

    public virtual DbSet<Repuesto> Repuestos { get; set; }

    public virtual DbSet<RespuestasEncuestum> RespuestasEncuesta { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TokensRecuperacionContrasena> TokensRecuperacionContrasenas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    public virtual DbSet<VideosCurso> VideosCursos { get; set; }

    public virtual DbSet<VwSaldoCuentaCliente> VwSaldoCuentaClientes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263

        => optionsBuilder.UseSqlServer("Server=RXBUSHIDO;Database=AOTechBD;Trusted_Connection=True;TrustServerCertificate=True");


    //nathanael conexion
    // optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AOTechBD;Trusted_Connection=True;TrustServerCertificate=True");



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogoServicio>(entity =>
        {
            entity.HasKey(e => e.ServicioId);

            entity.ToTable("CatalogoServicios", "Servicios");

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.DuracionEstimadaMinutos).HasDefaultValue(60);
            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PrecioMecanico).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PrecioRegular).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("Citas", "Citas");

            entity.HasIndex(e => e.ClienteId, "IX_Citas_ClienteId");

            entity.HasIndex(e => e.FechaSolicitada, "IX_Citas_FechaSolicitada");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Notas).HasMaxLength(300);

            entity.HasOne(d => d.AprobadaPorUsuario).WithMany(p => p.Cita)
                .HasForeignKey(d => d.AprobadaPorUsuarioId)
                .HasConstraintName("FK_Citas_AprobadaPor");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Clientes");

            entity.HasOne(d => d.Servicio).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ServicioId)
                .HasConstraintName("FK_Citas_CatalogoServicios");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Cita)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Vehiculos");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes", "Clientes");

            entity.HasIndex(e => e.CodigoClienteFrecuente, "UQ_Clientes_CodigoClienteFrecuente").IsUnique();

            entity.HasIndex(e => e.UsuarioId, "UQ_Clientes_UsuarioId").IsUnique();

            entity.Property(e => e.CodigoClienteFrecuente).HasMaxLength(20);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Telefono).HasMaxLength(20);

            entity.HasOne(d => d.Usuario).WithOne(p => p.Cliente)
                .HasForeignKey<Cliente>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Clientes_Usuarios");
        });

        modelBuilder.Entity<ComprasVideo>(entity =>
        {
            entity.HasKey(e => e.CompraId);

            entity.ToTable("ComprasVideo", "Cursos");

            entity.HasIndex(e => e.ClienteId, "IX_ComprasVideo_ClienteId");

            entity.HasIndex(e => new { e.ClienteId, e.VideoId }, "UQ_ComprasVideo_ClienteVideo").IsUnique();

            entity.Property(e => e.FechaCompra).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ComprasVideos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComprasVideo_Clientes");

            entity.HasOne(d => d.Video).WithMany(p => p.ComprasVideos)
                .HasForeignKey(d => d.VideoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComprasVideo_VideosCurso");
        });

        modelBuilder.Entity<Curso>(entity =>
        {
            entity.ToTable("Cursos", "Cursos");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Titulo).HasMaxLength(150);
        });

        modelBuilder.Entity<DetalleOrdenServicio>(entity =>
        {
            entity.ToTable("DetalleOrdenServicio", "Servicios");

            entity.HasIndex(e => e.OrdenServicioId, "IX_DetalleOrdenServicio_OrdenServicioId");

            entity.Property(e => e.Cantidad).HasDefaultValue(1);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Subtotal)
                .HasComputedColumnSql("([PrecioUnitario]*[Cantidad])", true)
                .HasColumnType("decimal(21, 2)");

            entity.HasOne(d => d.OrdenServicio).WithMany(p => p.DetalleOrdenServicios)
                .HasForeignKey(d => d.OrdenServicioId)
                .HasConstraintName("FK_DetalleOrdenServicio_OrdenesServicio");

            entity.HasOne(d => d.Servicio).WithMany(p => p.DetalleOrdenServicios)
                .HasForeignKey(d => d.ServicioId)
                .HasConstraintName("FK_DetalleOrdenServicio_CatalogoServicios");
        });

        modelBuilder.Entity<Encuesta>(entity =>
        {
            entity.ToTable("Encuestas", "Encuestas");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TipoCalificacion).HasMaxLength(20);
            entity.Property(e => e.Titulo).HasMaxLength(150);
        });

        modelBuilder.Entity<FechasBloqueada>(entity =>
        {
            entity.HasKey(e => e.FechaBloqueadaId);

            entity.ToTable("FechasBloqueadas", "Citas");

            entity.HasIndex(e => e.FechaBloqueada, "UQ_FechasBloqueadas_Fecha").IsUnique();

            entity.Property(e => e.Motivo).HasMaxLength(200);
        });

        modelBuilder.Entity<Membresia>(entity =>
        {
            entity.ToTable("Membresias", "Clientes");

            entity.HasIndex(e => e.ClienteId, "IX_Membresias_ClienteId");

            entity.HasIndex(e => e.CodigoBp, "UQ_Membresias_CodigoBP").IsUnique();

            entity.Property(e => e.CodigoBp)
                .HasMaxLength(20)
                .HasColumnName("CodigoBP");
            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TipoCliente).HasMaxLength(20);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Membresia)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Membresias_Clientes");
        });

        modelBuilder.Entity<MovimientosInventario>(entity =>
        {
            entity.HasKey(e => e.MovimientoId);

            entity.ToTable("MovimientosInventario", "Inventario");

            entity.HasIndex(e => e.RepuestoId, "IX_MovimientosInventario_RepuestoId");

            entity.Property(e => e.FechaMovimiento).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(200);
            entity.Property(e => e.TipoMovimiento).HasMaxLength(10);

            entity.HasOne(d => d.OrdenServicio).WithMany(p => p.MovimientosInventarios)
                .HasForeignKey(d => d.OrdenServicioId)
                .HasConstraintName("FK_MovimientosInventario_OrdenesServicio");

            entity.HasOne(d => d.Repuesto).WithMany(p => p.MovimientosInventarios)
                .HasForeignKey(d => d.RepuestoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosInventario_Repuestos");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosInventarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosInventario_Usuarios");
        });

        modelBuilder.Entity<OrdenesServicio>(entity =>
        {
            entity.HasKey(e => e.OrdenServicioId);

            entity.ToTable("OrdenesServicio", "Servicios");

            entity.HasIndex(e => e.ClienteId, "IX_OrdenesServicio_ClienteId");

            entity.HasIndex(e => e.MecanicoUsuarioId, "IX_OrdenesServicio_MecanicoUsuarioId");

            entity.HasIndex(e => e.VehiculoId, "IX_OrdenesServicio_VehiculoId");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Abierta");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FechaIngreso).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.OrdenesServicios)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesServicio_Clientes");

            entity.HasOne(d => d.MecanicoUsuario).WithMany(p => p.OrdenesServicioMecanicoUsuarios)
                .HasForeignKey(d => d.MecanicoUsuarioId)
                .HasConstraintName("FK_OrdenesServicio_Mecanico");

            entity.HasOne(d => d.RecepcionistaUsuario).WithMany(p => p.OrdenesServicioRecepcionistaUsuarios)
                .HasForeignKey(d => d.RecepcionistaUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesServicio_Recepcionista");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.OrdenesServicios)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesServicio_Vehiculos");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pagos", "Servicios");

            entity.HasIndex(e => e.ClienteId, "IX_Pagos_ClienteId");

            entity.HasIndex(e => e.OrdenServicioId, "IX_Pagos_OrdenServicioId");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Reportado");
            entity.Property(e => e.FechaReporte).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MetodoPago).HasMaxLength(30);
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Clientes");

            entity.HasOne(d => d.OrdenServicio).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.OrdenServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_OrdenesServicio");

            entity.HasOne(d => d.ValidadoPorUsuario).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.ValidadoPorUsuarioId)
                .HasConstraintName("FK_Pagos_ValidadoPor");
        });

        modelBuilder.Entity<PagosMembresium>(entity =>
        {
            entity.HasKey(e => e.PagoMembresiaId);

            entity.ToTable("PagosMembresia", "Clientes");

            entity.Property(e => e.FechaPago).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Membresia).WithMany(p => p.PagosMembresia)
                .HasForeignKey(d => d.MembresiaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PagosMembresia_Membresias");
        });

        modelBuilder.Entity<ReglasAgendum>(entity =>
        {
            entity.HasKey(e => e.ReglaId);

            entity.ToTable("ReglasAgenda", "Citas");

            entity.HasOne(d => d.Servicio).WithMany(p => p.ReglasAgenda)
                .HasForeignKey(d => d.ServicioId)
                .HasConstraintName("FK_ReglasAgenda_CatalogoServicios");
        });

        modelBuilder.Entity<Repuesto>(entity =>
        {
            entity.ToTable("Repuestos", "Inventario");

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<RespuestasEncuestum>(entity =>
        {
            entity.HasKey(e => e.RespuestaId);

            entity.ToTable("RespuestasEncuesta", "Encuestas");

            entity.HasIndex(e => e.EncuestaId, "IX_RespuestasEncuesta_EncuestaId");

            entity.HasIndex(e => new { e.EncuestaId, e.ClienteId, e.OrdenServicioId }, "UQ_RespuestasEncuesta_EncuestaClienteOrden").IsUnique();

            entity.Property(e => e.Calificacion).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.Comentarios).HasMaxLength(500);
            entity.Property(e => e.FechaRespuesta).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Cliente).WithMany(p => p.RespuestasEncuesta)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasEncuesta_Clientes");

            entity.HasOne(d => d.Encuesta).WithMany(p => p.RespuestasEncuesta)
                .HasForeignKey(d => d.EncuestaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasEncuesta_Encuestas");

            entity.HasOne(d => d.OrdenServicio).WithMany(p => p.RespuestasEncuesta)
                .HasForeignKey(d => d.OrdenServicioId)
                .HasConstraintName("FK_RespuestasEncuesta_OrdenesServicio");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId);

            entity.ToTable("Roles", "Seguridad");

            entity.HasIndex(e => e.NombreRol, "UQ_Roles_NombreRol").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.NombreRol).HasMaxLength(50);
        });

        modelBuilder.Entity<TokensRecuperacionContrasena>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.ToTable("TokensRecuperacionContrasena", "Seguridad");

            entity.HasIndex(e => e.Token, "UQ_TokensRecuperacionContrasena_Token").IsUnique();

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Token).HasMaxLength(256);

            entity.HasOne(d => d.Usuario).WithMany(p => p.TokensRecuperacionContrasenas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TokensRecuperacionContrasena_Usuarios");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", "Seguridad");

            entity.HasIndex(e => e.RolId, "IX_Usuarios_RolId");

            entity.HasIndex(e => e.Correo, "UQ_Usuarios_Correo").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "UQ_Usuarios_NombreUsuario").IsUnique();

            entity.Property(e => e.ContrasenaHash).HasMaxLength(256);
            entity.Property(e => e.ContrasenaSalt).HasMaxLength(128);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NombreCompleto).HasMaxLength(150);
            entity.Property(e => e.NombreUsuario).HasMaxLength(50);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.ToTable("Vehiculos", "Clientes");

            entity.HasIndex(e => e.ClienteId, "IX_Vehiculos_ClienteId");

            entity.HasIndex(e => e.Placa, "UQ_Vehiculos_Placa").IsUnique();

            entity.Property(e => e.EstaActivo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Marca).HasMaxLength(50);
            entity.Property(e => e.Modelo).HasMaxLength(50);
            entity.Property(e => e.Placa).HasMaxLength(15);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Vehiculos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehiculos_Clientes");
        });

        modelBuilder.Entity<VideosCurso>(entity =>
        {
            entity.HasKey(e => e.VideoId);

            entity.ToTable("VideosCurso", "Cursos");

            entity.HasIndex(e => e.CursoId, "IX_VideosCurso_CursoId");

            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Titulo).HasMaxLength(150);
            entity.Property(e => e.UrlVideo).HasMaxLength(500);

            entity.HasOne(d => d.Curso).WithMany(p => p.VideosCursos)
                .HasForeignKey(d => d.CursoId)
                .HasConstraintName("FK_VideosCurso_Cursos");
        });

        modelBuilder.Entity<VwSaldoCuentaCliente>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SaldoCuentaCliente", "Servicios");

            entity.Property(e => e.Saldo).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalCargos).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalPagado).HasColumnType("decimal(38, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
