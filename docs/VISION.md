# Visión y requisitos

## El problema

Una gincana o búsqueda del tesoro de un día (las fiestas de un pueblo, una empresa de actividades al aire libre)
hoy se monta con papel: pistas impresas, alguien en cada punto y un recuento a mano de quién llegó antes.
GincanaHud lo pasa al móvil: el jugador ve hacia dónde ir sobre la imagen de la cámara, el punto se da por
conseguido cuando está allí de verdad y el ranking sale solo.

También es un proyecto de portfolio y aprendizaje (MAUI, Aspire, DDD): se cuida que el código sea un buen ejemplo,
pero sin capas que no hagan falta.

## Usuarios

- **Jugador.** Usa su propio móvil Android, en la calle, a menudo con mala cobertura. No crea cuenta: se une con
  un código o un QR y pone su nombre (y un contacto si hay premio).
- **Admin de organización.** Prepara las actividades de su organización desde el ordenador, normalmente sin
  estar en el sitio, y sigue la partida el día del evento.
- **SuperAdmin** (el autor). Da de alta organizaciones y sus admins, y da soporte.

## Requisitos

### Jugador (app MAUI)

- Unirse a una actividad tecleando un código corto o escaneando su QR.
- Ver el siguiente POI con distancia y dirección sobre la cámara (HUD); aviso al acercarse.
- Capturar el POI al llegar: el servidor comprueba que está dentro del radio y en horario de juego.
- Seguir jugando sin cobertura: la ruta queda guardada en el móvil y las capturas se envían al volver la red,
  con la hora real de cada una.
- Dos modos de ruta: en orden fijo o libre (el POI pendiente más cercano) (D19).

### Admin de organización (panel web)

- Crear actividades con fecha de inicio y fin; fuera de ese horario no se puede capturar.
- Colocar los POIs en un mapa sin estar allí, buscando el lugar por nombre.
- Generar el código y el QR para carteles, email o WhatsApp.
- Ver en directo quién va por dónde y el ranking, con el contacto de cada jugador para los premios.
- Ver y limpiar los jugadores de una actividad.

### SuperAdmin

- Crear organizaciones y los admins de cada una. Cada organización solo ve lo suyo.

## Fuera de alcance

- Cuentas de jugador con contraseña, enlace mágico o SSO.
- iPhone (por ahora), realidad aumentada con ARKit/ARCore.
- Pagos, inscripciones o venta de entradas.
- Una base de datos por organización.
