Repositorio De Trabajo Relacionado Al proyecto AOTech

Se encontrará toda la documentación relacionada al mismo en este repositorio

en user-secrets. En Visual Studio: clic derecho en AOTech.Web Administrar secretos de usuario y pega:

{
  "ConnectionStrings": {
    "AOTechBD": "Server=TU_SERVIDOR;Database=AOTechBD;Trusted_Connection=True;TrustServerCertificate=True"
  }
}

Repite lo mismo en AOTech.API. Los secretos se cargan solos en Development y no se suben a Git.
