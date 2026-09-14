CASO 1

Se solicita analizar el siguiente caso de un torneo de fútbol, para el cual se requiere
diseñar la estructura de su sistema de información.
Se solicita analizar el caso de una liga deportiva que organiza un torneo de fútbol regional,
para el cual se requiere diseñar la estructura de un nuevo sistema de gestión de
competiciones.
En este torneo participan múltiples equipos, identificados por el nombre del club y su año
de fundación. Cada equipo está conformado por varios jugadores, de los cuales se
registra su número de documento, nombre completo, posición principal y fecha de
nacimiento.
Un jugador pertenece a un único equipo durante el torneo. La competición se desarrolla a
través de una serie de partidos. Cada partido enfrenta a dos equipos diferentes (uno
asume la condición de local y el otro de visitante), se juega en una fecha y hora
específicas, y se lleva a cabo en un estadio determinado, el cual se identifica por su
nombre, ciudad y capacidad máxima. Un estadio puede albergar diferentes partidos a lo
largo del campeonato. Cada equipo cuenta con un director técnico y personal médico a
los cuales se les registra su número de documento, nombre completo, profesión y/o
especialidad.
Adicionalmente, cada partido es dirigido por un grupo de árbitros. Un árbitro, identificado
por su código de licencia y nombre, puede pitar en varios partidos a lo largo del torneo,
desempeñando en cada uno distintos roles según su designación (como árbitro central,
asistente o VAR).
Durante el desarrollo de un partido, es fundamental registrar el rendimiento individual de
los futbolistas, almacenando la cantidad de goles anotados, tarjetas amarillas y tarjetas
rojas recibidas por un jugador específico en un partido determinado.

CASO 2

Se solicita analizar el caso de la cadena de minimercados &quot;El Huerto Feliz&quot;, un negocio
que combina el formato de frutería, verdulería y supermercado tradicional, para el cual se
requiere diseñar la estructura central de su sistema de ventas e inventario.
El minimercado comercializa una amplia variedad de productos, de los cuales se registra
su código de barras, nombre, categoría (Fruta, Verdura, Abarrote) y precio de venta al
público. Para abastecer, la cadena negocia con diferentes proveedores (identificados por
su NIT y razón social).

Dado que un mismo producto puede ser suministrado por varios proveedores y un
proveedor entrega múltiples productos, el sistema debe registrar el precio de compra base
acordado para cada combinación de producto y proveedor.
Por otro lado, la cadena opera a través de diversas sucursales físicas, identificadas por un
número de local y dirección. El sistema debe controlar el inventario de forma
independiente, es decir, debe registrar la cantidad disponible de cada producto en cada
sucursal específica.
Para fidelizar a los compradores, el minimercado registra a sus clientes frecuentes,
guardando su documento, nombre completo y puntos acumulados. La operación principal
del día a día es la venta; cuando un cliente realiza sus compras en una sucursal, el
sistema genera una factura de venta, la cual posee un número único, fecha, hora y el
valor total a pagar. Una factura agrupa la compra de varios productos, y de cada producto
vendido en esa transacción se debe registrar la cantidad exacta que el cliente llevó y el
subtotal calculado.

CASO 3

La Institución Educativa “Pequeños traviesos”, le ha encomendado al Politécnico JIC el
desarrollo de una aplicación Web y móvil para la gestión de su restaurante escolar.
Según nos explica el rector y el administrador del restaurante, a la aplicación pueden
entrar los estudiantes compuesto por niños de los 6 a 8 años, 9 a 12 y adolescentes de 13
a 17 años, en la aplicación se debe registrar el nombre, la edad, el número de la tarjeta de
identidad, restricciones de alimentos, alergias. Éstos pueden ingresar para conocer la
minuta semanal; los padres de familia también la pueden visualizar y pueden cambiar el
menú de sus hijos y reservar, adicionalmente puede realizar el pago por cada uno de sus
hijos dependiendo del menú.
Los empleados pueden hacer consultas, reservas de almuerzo y pagar anticipadamente
teniendo en cuenta que pueden pagar de forma remota sin la necesidad de recurrir a la
institución o al banco. Se desea que el sistema de reservas sea accesible a través de la
web o desde un celular.
El sistema actualmente tiene un terminal de servicio de reserva en donde se presenta un
mensaje de bienvenida describiendo los servicios ofrecidos junto con la opción de
registrarse por primera vez, o si ya está registrado, poder utilizar el sistema de reserva.
Este acceso se da por medio de la inserción de un usuario y contraseña previamente
especificado (dirección de correo del usuario) y una contraseña previamente escogida y
que debe validarse.
Una vez registrado el usuario y después de haberse validado el registro y contraseña del
usuario, se pueden seleccionar actividades como: Consultar la minuta semanal o el menú
del día, el menú debe especificar los alimentos que compone cada plato.

Reservar o comprar almuerzo o media mañana, o pagar entre otros.
La consulta de las minutas semanales o almuerzos del día se puede hacer de tres
maneras
diferentes: tarifas o precios, información detallada del almuerzo o media mañana o por
productos.
La consulta por productos, muestra los productos de las diferentes marcas o
características
nutricionales, indicando el costo y la existencia.
La reserva de los almuerzos permite al usuario seleccionar lo que desea comer,
especificando fecha, hora, menú y pago.
El rector nos comenta que los empleados tienen un descuento del 20%.
Es beneficioso para los padres de familia porque conocerán diariamente lo que consumen
sus hijos y en caso de desear cambiar el menú, la aplicación lo debe permitir.

CASO 4

Se solicita analizar el caso de la aerolínea &quot;Alas del Poblado&quot;, para la cual se requiere
diseñar la estructura central de su nuevo sistema de gestión de vuelos y reservas. La
aerolínea cuenta con una flota de aviones, cada uno identificado por su matrícula, modelo
y capacidad máxima de pasajeros y servicios (internet, tv entre otros).
Estos aviones operan entre diversos aeropuertos, los cuales se identifican por su código,
nombre y ciudad donde están ubicados. La operación principal de la empresa se basa en
la programación de vuelos; cada vuelo tiene asignado un número de vuelo único, una
fecha y hora de salida, una fecha y hora de llegada estimada, y se realiza en un avión
específico.
Además, todo vuelo debe registrar obligatoriamente cuál es su aeropuerto de origen y
cuál es su aeropuerto de destino. Para operar un vuelo, se requiere asignar a un grupo de
tripulantes. Cada tripulante posee un número de identificación y nombre completo, y al ser
asignado a un vuelo específico, desempeña un rol particular (Piloto, Copiloto o Auxiliar de
Vuelo).
Por otro lado, los clientes de la aerolínea son los pasajeros, de quienes se registra su
número de pasaporte o cédula de ciudadanía si es Colombiano, nombre completo y
nacionalidad.

Un pasajero puede viajar en muchos vuelos a lo largo del tiempo, y un vuelo obviamente
transporta a múltiples pasajeros. Cuando un pasajero decide viajar en un vuelo específico,
el sistema genera una reserva formal (tiquete), la cual guarda información vital de ese
viaje en particular: el número de asiento asignado, la clase de la cabina (Económica o
Ejecutiva) y el precio final pagado.