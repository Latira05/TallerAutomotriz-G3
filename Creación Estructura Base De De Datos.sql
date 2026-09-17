/* ============================================================================
   Script de creación de base de datos - Taller A.O Tech
   Motor: SQL Server
   Convenciones:
     - Nombres de base de datos, esquemas, tablas y columnas en español
     - Esquemas por módulo para orden y seguridad de permisos
     - Baja lógica (EstaActivo) en catálogos maestros, nunca DELETE físico
     - Campos de auditoría (FechaCreacion/FechaActualizacion) en tablas transaccionales
   ============================================================================ */

IF DB_ID(N'AOTechBD') IS NULL
BEGIN
    CREATE DATABASE AOTechBD;
END
GO

USE AOTechBD;
GO

/* ============================================================================
   1. ESQUEMAS (uno por módulo, para separar permisos y mantener orden)
   ============================================================================ */
CREATE SCHEMA Seguridad AUTHORIZATION dbo;
GO
CREATE SCHEMA Clientes AUTHORIZATION dbo;
GO
CREATE SCHEMA Servicios AUTHORIZATION dbo;
GO
CREATE SCHEMA Citas AUTHORIZATION dbo;
GO
CREATE SCHEMA Inventario AUTHORIZATION dbo;
GO
CREATE SCHEMA Cursos AUTHORIZATION dbo;
GO
CREATE SCHEMA Encuestas AUTHORIZATION dbo;
GO

/* ============================================================================
   2. SEGURIDAD: Roles, Usuarios, recuperación de contraseña
   ============================================================================ */
CREATE TABLE Seguridad.Roles (
    RolId               INT IDENTITY(1,1)  NOT NULL,
    NombreRol           NVARCHAR(50)       NOT NULL,
    Descripcion         NVARCHAR(200)      NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RolId),
    CONSTRAINT UQ_Roles_NombreRol UNIQUE (NombreRol)
);
GO

CREATE TABLE Seguridad.Usuarios (
    UsuarioId           INT IDENTITY(1,1)  NOT NULL,
    RolId                INT               NOT NULL,
    NombreUsuario        NVARCHAR(50)      NOT NULL,
    Correo               NVARCHAR(150)     NOT NULL,
    ContrasenaHash       VARBINARY(256)    NOT NULL,
    ContrasenaSalt       VARBINARY(128)    NOT NULL,
    NombreCompleto       NVARCHAR(150)     NOT NULL,
    EstaActivo           BIT               NOT NULL DEFAULT (1),
    FechaCreacion        DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion   DATETIME2         NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT UQ_Usuarios_Correo UNIQUE (Correo),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES Seguridad.Roles (RolId)
);
GO

CREATE TABLE Seguridad.TokensRecuperacionContrasena (
    TokenId             INT IDENTITY(1,1)  NOT NULL,
    UsuarioId           INT                NOT NULL,
    Token               NVARCHAR(256)      NOT NULL,
    FechaExpiracion     DATETIME2          NOT NULL,
    FechaUso            DATETIME2          NULL,
    FechaCreacion       DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_TokensRecuperacionContrasena PRIMARY KEY (TokenId),
    CONSTRAINT UQ_TokensRecuperacionContrasena_Token UNIQUE (Token),
    CONSTRAINT FK_TokensRecuperacionContrasena_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId)
);
GO

/* ============================================================================
   3. CLIENTES: Clientes, Vehículos, Membresías / Cartera (código BP)
   ============================================================================ */
CREATE TABLE Clientes.Clientes (
    ClienteId               INT IDENTITY(1,1)  NOT NULL,
    UsuarioId               INT                NOT NULL,
    CodigoClienteFrecuente  NVARCHAR(20)       NOT NULL,
    Telefono                NVARCHAR(20)       NULL,
    Direccion               NVARCHAR(250)      NULL,
    EstaActivo              BIT                NOT NULL DEFAULT (1),
    FechaCreacion           DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Clientes PRIMARY KEY (ClienteId),
    CONSTRAINT UQ_Clientes_UsuarioId UNIQUE (UsuarioId),
    CONSTRAINT UQ_Clientes_CodigoClienteFrecuente UNIQUE (CodigoClienteFrecuente),
    CONSTRAINT FK_Clientes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId)
);
GO

CREATE TABLE Clientes.Vehiculos (
    VehiculoId      INT IDENTITY(1,1)      NOT NULL,
    ClienteId       INT                    NOT NULL,
    Placa           NVARCHAR(15)           NOT NULL,
    Marca           NVARCHAR(50)           NOT NULL,
    Modelo          NVARCHAR(50)           NOT NULL,
    Anio            SMALLINT               NULL,
    Kilometraje     INT                    NOT NULL DEFAULT (0),
    EstaActivo      BIT                    NOT NULL DEFAULT (1),
    FechaCreacion   DATETIME2              NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Vehiculos PRIMARY KEY (VehiculoId),
    CONSTRAINT UQ_Vehiculos_Placa UNIQUE (Placa),
    CONSTRAINT FK_Vehiculos_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT CK_Vehiculos_Kilometraje CHECK (Kilometraje >= 0)
);
GO

CREATE TABLE Clientes.Membresias (
    MembresiaId     INT IDENTITY(1,1)      NOT NULL,
    ClienteId       INT                    NOT NULL,
    CodigoBP        NVARCHAR(20)           NOT NULL,
    TipoCliente     NVARCHAR(20)           NOT NULL,
    FechaInicio     DATE                   NOT NULL,
    FechaFin        DATE                   NOT NULL,
    EstaActivo      BIT                    NOT NULL DEFAULT (1),
    FechaCreacion   DATETIME2              NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Membresias PRIMARY KEY (MembresiaId),
    CONSTRAINT UQ_Membresias_CodigoBP UNIQUE (CodigoBP),
    CONSTRAINT FK_Membresias_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT CK_Membresias_TipoCliente CHECK (TipoCliente IN ('Regular','Mecanico')),
    CONSTRAINT CK_Membresias_Fechas CHECK (FechaFin >= FechaInicio)
);
GO

CREATE TABLE Clientes.PagosMembresia (
    PagoMembresiaId INT IDENTITY(1,1)      NOT NULL,
    MembresiaId     INT                    NOT NULL,
    Monto           DECIMAL(10,2)          NOT NULL,
    FechaPago       DATETIME2              NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PagosMembresia PRIMARY KEY (PagoMembresiaId),
    CONSTRAINT FK_PagosMembresia_Membresias FOREIGN KEY (MembresiaId) REFERENCES Clientes.Membresias (MembresiaId),
    CONSTRAINT CK_PagosMembresia_Monto CHECK (Monto >= 0)
);
GO

/* ============================================================================
   4. SERVICIOS: Catálogo, órdenes de servicio, detalle, pagos
   ============================================================================ */
CREATE TABLE Servicios.CatalogoServicios (
    ServicioId                  INT IDENTITY(1,1)  NOT NULL,
    Nombre                      NVARCHAR(100)      NOT NULL,
    Descripcion                 NVARCHAR(300)      NULL,
    PrecioRegular                DECIMAL(10,2)     NOT NULL,
    PrecioMecanico               DECIMAL(10,2)     NOT NULL,
    DuracionEstimadaMinutos      INT               NOT NULL DEFAULT (60),
    EstaActivo                   BIT               NOT NULL DEFAULT (1),
    CONSTRAINT PK_CatalogoServicios PRIMARY KEY (ServicioId),
    CONSTRAINT CK_CatalogoServicios_PrecioRegular CHECK (PrecioRegular >= 0),
    CONSTRAINT CK_CatalogoServicios_PrecioMecanico CHECK (PrecioMecanico >= 0)
);
GO

CREATE TABLE Servicios.OrdenesServicio (
    OrdenServicioId          INT IDENTITY(1,1)     NOT NULL,
    VehiculoId               INT                   NOT NULL,
    ClienteId                INT                   NOT NULL,
    RecepcionistaUsuarioId   INT                   NOT NULL,
    MecanicoUsuarioId        INT                   NULL,
    Estado                   NVARCHAR(20)          NOT NULL DEFAULT ('Abierta'),
    FechaIngreso             DATETIME2             NOT NULL DEFAULT (SYSUTCDATETIME()),
    FechaEntregaEstimada     DATETIME2             NULL,
    Descripcion              NVARCHAR(500)         NULL,
    MontoTotal               DECIMAL(10,2)         NOT NULL DEFAULT (0),
    FechaCreacion            DATETIME2             NOT NULL DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion       DATETIME2             NULL,
    CONSTRAINT PK_OrdenesServicio PRIMARY KEY (OrdenServicioId),
    CONSTRAINT FK_OrdenesServicio_Vehiculos FOREIGN KEY (VehiculoId) REFERENCES Clientes.Vehiculos (VehiculoId),
    CONSTRAINT FK_OrdenesServicio_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT FK_OrdenesServicio_Recepcionista FOREIGN KEY (RecepcionistaUsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId),
    CONSTRAINT FK_OrdenesServicio_Mecanico FOREIGN KEY (MecanicoUsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId),
    CONSTRAINT CK_OrdenesServicio_Estado CHECK (Estado IN ('Abierta','EnProceso','Finalizada','Cancelada')),
    CONSTRAINT CK_OrdenesServicio_MontoTotal CHECK (MontoTotal >= 0)
);
GO

CREATE TABLE Servicios.DetalleOrdenServicio (
    DetalleOrdenServicioId     INT IDENTITY(1,1)   NOT NULL,
    OrdenServicioId            INT                 NOT NULL,
    ServicioId                 INT                 NULL,
    Descripcion                NVARCHAR(200)       NOT NULL,
    PrecioUnitario              DECIMAL(10,2)      NOT NULL,
    Cantidad                    INT                NOT NULL DEFAULT (1),
    Subtotal AS (PrecioUnitario * Cantidad) PERSISTED,
    CONSTRAINT PK_DetalleOrdenServicio PRIMARY KEY (DetalleOrdenServicioId),
    CONSTRAINT FK_DetalleOrdenServicio_OrdenesServicio FOREIGN KEY (OrdenServicioId) REFERENCES Servicios.OrdenesServicio (OrdenServicioId) ON DELETE CASCADE,
    CONSTRAINT FK_DetalleOrdenServicio_CatalogoServicios FOREIGN KEY (ServicioId) REFERENCES Servicios.CatalogoServicios (ServicioId),
    CONSTRAINT CK_DetalleOrdenServicio_PrecioUnitario CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_DetalleOrdenServicio_Cantidad CHECK (Cantidad > 0)
);
GO

CREATE TABLE Servicios.Pagos (
    PagoId               INT IDENTITY(1,1)         NOT NULL,
    OrdenServicioId      INT                       NOT NULL,
    ClienteId            INT                       NOT NULL,
    Monto                DECIMAL(10,2)             NOT NULL,
    MetodoPago           NVARCHAR(30)              NOT NULL,
    Estado               NVARCHAR(20)              NOT NULL DEFAULT ('Reportado'),
    FechaReporte         DATETIME2                 NOT NULL DEFAULT (SYSUTCDATETIME()),
    ValidadoPorUsuarioId INT                       NULL,
    FechaValidacion      DATETIME2                 NULL,
    CONSTRAINT PK_Pagos PRIMARY KEY (PagoId),
    CONSTRAINT FK_Pagos_OrdenesServicio FOREIGN KEY (OrdenServicioId) REFERENCES Servicios.OrdenesServicio (OrdenServicioId),
    CONSTRAINT FK_Pagos_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT FK_Pagos_ValidadoPor FOREIGN KEY (ValidadoPorUsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto >= 0),
    CONSTRAINT CK_Pagos_Estado CHECK (Estado IN ('Reportado','Validado','Rechazado'))
);
GO

/* ============================================================================
   5. CITAS: Reglas de agenda, fechas bloqueadas, citas
   ============================================================================ */
CREATE TABLE Citas.ReglasAgenda (
    ReglaId             INT IDENTITY(1,1)  NOT NULL,
    DiaSemana           TINYINT            NOT NULL,
    HoraApertura        TIME               NOT NULL,
    HoraCierre          TIME               NOT NULL,
    CapacidadBahias     INT                NOT NULL,
    ServicioId          INT                NULL,
    CONSTRAINT PK_ReglasAgenda PRIMARY KEY (ReglaId),
    CONSTRAINT FK_ReglasAgenda_CatalogoServicios FOREIGN KEY (ServicioId) REFERENCES Servicios.CatalogoServicios (ServicioId),
    CONSTRAINT CK_ReglasAgenda_DiaSemana CHECK (DiaSemana BETWEEN 0 AND 6),
    CONSTRAINT CK_ReglasAgenda_Horas CHECK (HoraCierre > HoraApertura),
    CONSTRAINT CK_ReglasAgenda_CapacidadBahias CHECK (CapacidadBahias > 0)
);
GO

CREATE TABLE Citas.FechasBloqueadas (
    FechaBloqueadaId    INT IDENTITY(1,1)  NOT NULL,
    FechaBloqueada      DATE               NOT NULL,
    Motivo              NVARCHAR(200)      NULL,
    CONSTRAINT PK_FechasBloqueadas PRIMARY KEY (FechaBloqueadaId),
    CONSTRAINT UQ_FechasBloqueadas_Fecha UNIQUE (FechaBloqueada)
);
GO

CREATE TABLE Citas.Citas (
    CitaId                  INT IDENTITY(1,1)      NOT NULL,
    ClienteId               INT                    NOT NULL,
    VehiculoId              INT                    NOT NULL,
    ServicioId              INT                    NULL,
    FechaSolicitada         DATE                   NOT NULL,
    HoraSolicitada          TIME                   NOT NULL,
    Estado                  NVARCHAR(20)           NOT NULL DEFAULT ('Pendiente'),
    AprobadaPorUsuarioId    INT                    NULL,
    Notas                   NVARCHAR(300)          NULL,
    FechaCreacion           DATETIME2              NOT NULL DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion      DATETIME2              NULL,
    CONSTRAINT PK_Citas PRIMARY KEY (CitaId),
    CONSTRAINT FK_Citas_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT FK_Citas_Vehiculos FOREIGN KEY (VehiculoId) REFERENCES Clientes.Vehiculos (VehiculoId),
    CONSTRAINT FK_Citas_CatalogoServicios FOREIGN KEY (ServicioId) REFERENCES Servicios.CatalogoServicios (ServicioId),
    CONSTRAINT FK_Citas_AprobadaPor FOREIGN KEY (AprobadaPorUsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId),
    CONSTRAINT CK_Citas_Estado CHECK (Estado IN ('Pendiente','Aprobada','Reprogramada','Cancelada'))
);
GO

/* ============================================================================
   6. INVENTARIO: Repuestos y movimientos
   ============================================================================ */
CREATE TABLE Inventario.Repuestos (
    RepuestoId      INT IDENTITY(1,1)  NOT NULL,
    Nombre          NVARCHAR(100)      NOT NULL,
    Descripcion     NVARCHAR(300)      NULL,
    PrecioUnitario  DECIMAL(10,2)      NOT NULL,
    CantidadStock   INT                NOT NULL DEFAULT (0),
    StockMinimo     INT                NOT NULL DEFAULT (0),
    EstaActivo      BIT                NOT NULL DEFAULT (1),
    FechaCreacion   DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Repuestos PRIMARY KEY (RepuestoId),
    CONSTRAINT CK_Repuestos_PrecioUnitario CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_Repuestos_CantidadStock CHECK (CantidadStock >= 0)
);
GO

CREATE TABLE Inventario.MovimientosInventario (
    MovimientoId    INT IDENTITY(1,1)  NOT NULL,
    RepuestoId      INT                NOT NULL,
    TipoMovimiento  NVARCHAR(10)       NOT NULL,
    Cantidad        INT                NOT NULL,
    OrdenServicioId INT                NULL,
    UsuarioId       INT                NOT NULL,
    Motivo          NVARCHAR(200)      NULL,
    FechaMovimiento DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_MovimientosInventario PRIMARY KEY (MovimientoId),
    CONSTRAINT FK_MovimientosInventario_Repuestos FOREIGN KEY (RepuestoId) REFERENCES Inventario.Repuestos (RepuestoId),
    CONSTRAINT FK_MovimientosInventario_OrdenesServicio FOREIGN KEY (OrdenServicioId) REFERENCES Servicios.OrdenesServicio (OrdenServicioId),
    CONSTRAINT FK_MovimientosInventario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios (UsuarioId),
    CONSTRAINT CK_MovimientosInventario_Tipo CHECK (TipoMovimiento IN ('Entrada','Salida')),
    CONSTRAINT CK_MovimientosInventario_Cantidad CHECK (Cantidad > 0)
);
GO

/* ============================================================================
   7. CURSOS: Catálogo de cursos, videos, compras (paywall)
   ============================================================================ */
CREATE TABLE Cursos.Cursos (
    CursoId         INT IDENTITY(1,1)  NOT NULL,
    Titulo          NVARCHAR(150)      NOT NULL,
    Descripcion     NVARCHAR(500)      NULL,
    EstaPublicado   BIT                NOT NULL DEFAULT (0),
    FechaCreacion   DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Cursos PRIMARY KEY (CursoId)
);
GO

CREATE TABLE Cursos.VideosCurso (
    VideoId             INT IDENTITY(1,1)  NOT NULL,
    CursoId             INT                NOT NULL,
    Titulo              NVARCHAR(150)      NOT NULL,
    Precio              DECIMAL(10,2)      NOT NULL,
    UrlVideo            NVARCHAR(500)      NOT NULL,
    DuracionMinutos     INT                NULL,
    CONSTRAINT PK_VideosCurso PRIMARY KEY (VideoId),
    CONSTRAINT FK_VideosCurso_Cursos FOREIGN KEY (CursoId) REFERENCES Cursos.Cursos (CursoId) ON DELETE CASCADE,
    CONSTRAINT CK_VideosCurso_Precio CHECK (Precio >= 0)
);
GO

CREATE TABLE Cursos.ComprasVideo (
    CompraId        INT IDENTITY(1,1)  NOT NULL,
    ClienteId       INT                NOT NULL,
    VideoId         INT                NOT NULL,
    Monto           DECIMAL(10,2)      NOT NULL,
    FechaCompra     DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ComprasVideo PRIMARY KEY (CompraId),
    CONSTRAINT FK_ComprasVideo_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT FK_ComprasVideo_VideosCurso FOREIGN KEY (VideoId) REFERENCES Cursos.VideosCurso (VideoId),
    CONSTRAINT UQ_ComprasVideo_ClienteVideo UNIQUE (ClienteId, VideoId),
    CONSTRAINT CK_ComprasVideo_Monto CHECK (Monto >= 0)
);
GO

/* ============================================================================
   8. ENCUESTAS: Encuestas de satisfacción y respuestas
   ============================================================================ */
CREATE TABLE Encuestas.Encuestas (
    EncuestaId          INT IDENTITY(1,1)  NOT NULL,
    Titulo              NVARCHAR(150)      NOT NULL,
    TipoCalificacion    NVARCHAR(20)       NOT NULL,
    EstaPublicada       BIT                NOT NULL DEFAULT (0),
    FechaCreacion       DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Encuestas PRIMARY KEY (EncuestaId),
    CONSTRAINT CK_Encuestas_TipoCalificacion CHECK (TipoCalificacion IN ('Numerica','Estrellas'))
);
GO

CREATE TABLE Encuestas.RespuestasEncuesta (
    RespuestaId     INT IDENTITY(1,1)  NOT NULL,
    EncuestaId      INT                NOT NULL,
    ClienteId       INT                NOT NULL,
    OrdenServicioId INT                NULL,
    Calificacion    DECIMAL(3,1)       NOT NULL,
    Comentarios     NVARCHAR(500)      NULL,
    FechaRespuesta  DATETIME2          NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_RespuestasEncuesta PRIMARY KEY (RespuestaId),
    CONSTRAINT FK_RespuestasEncuesta_Encuestas FOREIGN KEY (EncuestaId) REFERENCES Encuestas.Encuestas (EncuestaId),
    CONSTRAINT FK_RespuestasEncuesta_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes.Clientes (ClienteId),
    CONSTRAINT FK_RespuestasEncuesta_OrdenesServicio FOREIGN KEY (OrdenServicioId) REFERENCES Servicios.OrdenesServicio (OrdenServicioId),
    CONSTRAINT UQ_RespuestasEncuesta_EncuestaClienteOrden UNIQUE (EncuestaId, ClienteId, OrdenServicioId)
);
GO

/* ============================================================================
   9. ÍNDICES ADICIONALES sobre llaves foráneas (mejoran JOINs y filtros)
   ============================================================================ */
CREATE INDEX IX_Usuarios_RolId ON Seguridad.Usuarios (RolId);
CREATE INDEX IX_Vehiculos_ClienteId ON Clientes.Vehiculos (ClienteId);
CREATE INDEX IX_Membresias_ClienteId ON Clientes.Membresias (ClienteId);
CREATE INDEX IX_OrdenesServicio_ClienteId ON Servicios.OrdenesServicio (ClienteId);
CREATE INDEX IX_OrdenesServicio_VehiculoId ON Servicios.OrdenesServicio (VehiculoId);
CREATE INDEX IX_OrdenesServicio_MecanicoUsuarioId ON Servicios.OrdenesServicio (MecanicoUsuarioId);
CREATE INDEX IX_DetalleOrdenServicio_OrdenServicioId ON Servicios.DetalleOrdenServicio (OrdenServicioId);
CREATE INDEX IX_Pagos_OrdenServicioId ON Servicios.Pagos (OrdenServicioId);
CREATE INDEX IX_Pagos_ClienteId ON Servicios.Pagos (ClienteId);
CREATE INDEX IX_Citas_ClienteId ON Citas.Citas (ClienteId);
CREATE INDEX IX_Citas_FechaSolicitada ON Citas.Citas (FechaSolicitada);
CREATE INDEX IX_MovimientosInventario_RepuestoId ON Inventario.MovimientosInventario (RepuestoId);
CREATE INDEX IX_VideosCurso_CursoId ON Cursos.VideosCurso (CursoId);
CREATE INDEX IX_ComprasVideo_ClienteId ON Cursos.ComprasVideo (ClienteId);
CREATE INDEX IX_RespuestasEncuesta_EncuestaId ON Encuestas.RespuestasEncuesta (EncuestaId);
GO

/* ============================================================================
   10. VISTA: Estado de cuenta / saldo por cliente
       (cargos de órdenes finalizadas vs. pagos validados)
   ============================================================================ */
CREATE VIEW Servicios.vw_SaldoCuentaCliente AS
SELECT
    c.ClienteId,
    ISNULL(cargos.TotalCargos, 0)  AS TotalCargos,
    ISNULL(pagado.TotalPagado, 0)  AS TotalPagado,
    ISNULL(cargos.TotalCargos, 0) - ISNULL(pagado.TotalPagado, 0) AS Saldo
FROM Clientes.Clientes c
LEFT JOIN (
    SELECT os.ClienteId, SUM(os.MontoTotal) AS TotalCargos
    FROM Servicios.OrdenesServicio os
    WHERE os.Estado = 'Finalizada'
    GROUP BY os.ClienteId
) cargos ON cargos.ClienteId = c.ClienteId
LEFT JOIN (
    SELECT p.ClienteId, SUM(p.Monto) AS TotalPagado
    FROM Servicios.Pagos p
    WHERE p.Estado = 'Validado'
    GROUP BY p.ClienteId
) pagado ON pagado.ClienteId = c.ClienteId;
GO

/* ============================================================================
   11. DATOS SEMILLA MÍNIMOS (roles base del sistema)
   ============================================================================ */
INSERT INTO Seguridad.Roles (NombreRol, Descripcion) VALUES
    (N'Administrador', N'Control total del sistema'),
    (N'Recepcionista', N'Gestión de clientes, vehículos, citas y órdenes'),
    (N'Mecanico', N'Ejecución y actualización de órdenes de servicio e inventario'),
    (N'Cliente', N'Acceso a su información, citas, pagos y cursos');
GO