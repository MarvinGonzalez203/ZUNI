# Entrega preparada — ZUNI 1.6.2 para MobaXterm

18/11/2026. Alcance confirmado: ZUNI y portada Denis Corp sobre Linode / Debian 13. No se activan otras aplicaciones ni se modifica POS.

- Commit del paquete: `9386dc1d3c0e8c2e3a97cc27490b3e5cb11be326`.
- ZIP local: `.visual-check/despliegue-1.6.2/20261118-164453-2264df/zuni-1.6.2-mobaxterm.zip`.
- Tamaño: 16767164 bytes.
- SHA-256 ZIP: `1dbd5192842d4adc5e406f72153edb09ad6ac24619920559b829cd783d760c45`.
- SHA-256 publicación tar: `84766ee862e55e87171c315dd941424a1741ac1930ba0ec999cf46ad14e13bce`.
- SHA-256 manifiesto: `fb3499c0ce9f20513314598ea0fbf9fedf11288ffc8f685dda35abba98e62569`.
- Comprobador: 26 controles; PostgreSQL local: 17 comprobaciones ficticias aprobadas, objetos descartables retirados y laboratorio detenido.
- Manifiesto: 31 archivos verificados; ZIP: 32 archivos con contenido idéntico a la entrega; tar: 414 entradas regulares/directorios. Scripts Bash sin BOM/CRLF; SQL UTF-8. Los archivos de texto del archivo Git respetan su conversión normal de finales de línea; se compararon normalizados con el commit.

[Instrucciones completas](despliegue-1.6.2.md). Extraer y copiar la carpeta `actualizacion-1.6.2` completa al SFTP en `/home/marlon/zuni-subida/`. Después ejecutar:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --comprobar
```

Una vez obtenido `PREPARADO`, activar en mantenimiento con:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --aplicar
```

El paquete está preparado en el PC, **no subido ni activado**. No se instaló runtime, cambió Nginx/DNS, abrió conexión SQL ni ejecutó systemd en Linode. El servidor público respondió 200 con título ZUNI antes de la preparación, sin identificar la versión. El diagnóstico real y el ensayo Debian se hacen allí antes de aplicar a la base viva.

Las tres modificaciones locales previas de privacidad permanecen fuera del paquete y del commit de herramientas. UI-03 y la configuración de Brevo continúan con los pendientes documentados. El ZIP y sus datos de construcción quedan ignorados; a Git se suben herramientas, documentación y sumas, nunca bases, claves ni configuraciones privadas.
