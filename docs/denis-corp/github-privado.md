# Denis Corp privado en GitHub

Un repositorio **Private** bajo tu cuenta personal puede quedar accesible solo para
ti si no das acceso a colaboradores ni aplicaciones adicionales. Otra cuenta sin
permiso no puede consultar ni clonar ese repositorio. Una persona o aplicación con
acceso autorizado puede leer y copiar el código; retirar el permiso no borra las
copias locales que ya tenga. [Permisos oficiales de GitHub](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/repository-access-and-collaboration/permission-levels-for-a-personal-account-repository).

La privacidad corresponde al **repositorio completo**. Una rama `main` o `zuni` no
tiene un permiso de lectura independiente. Las reglas de ramas controlan cambios
y fusiones, no convierten una rama en un espacio secreto frente a quien puede leer
el repositorio. Publicar la web tampoco requiere publicar el repositorio; el HTML,
CSS y JavaScript que se sirven al navegador siguen siendo visibles para sus visitantes.

## Crear el repositorio y main

1. En GitHub inicia sesión con tu cuenta personal y abre **+ → New repository**.
2. En **Owner**, elige tu cuenta. Nombre sugerido: `denis-corp`.
3. Marca **Private**. Para un repositorio nuevo puedes inicializar un README.
   Si vas a importar un historial existente, crea el destino vacío para evitar
   un historial inicial diferente.
4. Pulsa **Create repository** y comprueba el indicador **Private**.
5. En **Settings → Collaborators**, conserva el acceso únicamente para ti.
   Revisa también aplicaciones instaladas, claves de despliegue y tokens autorizados.
6. En **Settings → General → Default branch**, comprueba que la rama predeterminada
   sea `main`. Activa 2FA en tu cuenta de GitHub.
7. Abre GitHub Desktop con la misma cuenta y usa **File → Clone repository**
   para seleccionar el repositorio privado.

Los pasos de creación están basados en la [documentación oficial de GitHub](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository).
Este trabajo solo entrega las instrucciones; no ha creado ni publicado un nuevo
repositorio remoto ni ha cambiado el remote de ZUNI.

## Ramas por producto, como solicitaste

En GitHub Desktop, para cada producto:

1. Selecciona **Current Branch → main**.
2. Pulsa **New Branch**.
3. Escribe `producto/zuni`, `producto/academy`, `producto/fulleo`, `producto/pos`
   o `producto/colegios`, y elige **main** como base.
4. Pulsa **Create Branch** y después **Publish branch**.
5. Vuelve a `main` antes de crear la siguiente para que todas partan de la misma base.

Las ramas publicadas heredan la privacidad del repositorio.
[Crear y publicar ramas en GitHub Desktop](https://docs.github.com/en/desktop/making-changes-in-a-branch/managing-branches-in-github-desktop).

## Organización recomendada del código

Para mantener varias aplicaciones juntas, conviene que `main` represente el conjunto
estable y que cada programa ocupe su carpeta. Las ramas sirven para desarrollar
cambios y luego integrarlos, no para mantener aplicaciones independientes que se
sobrescriben al cambiar de rama.

```text
denis-corp/                 repositorio privado
  README.md                índice común
  docs/productos/           propósito, estado, arranque y pendientes
  apps/portal/
  apps/zuni/
  apps/academy/
  apps/fulleo/
  apps/pos/
  apps/colegios/
```

Si en el futuro necesitas dar acceso a una persona solo a un producto, utiliza
repositorios privados separados para cada aplicación y conserva `denis-corp` como
portal e índice común. Todos pueden tener su propia rama `main`.

La migración del código e historial a esta estructura es una tarea separada.
Conserva los repositorios e historiales actuales hasta comprobar la importación.
El contenido a publicar debe excluir `.env`, credenciales, claves de cifrado,
bases de datos, respaldos y expedientes reales, incluso cuando el repositorio sea privado.
