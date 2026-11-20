# Sonidos de CodeOS

Los sonidos de la interfaz de CodeOS, uno por acción: `enter`, `move`, `tab`, `select`, `back`, `launch`, `panel`, `panelClose`, `home` y
`notify` (en `.flac`, `.wav`, `.mp3`, `.m4a` o `.aiff`). Xcode los mete en `Jack.app/Contents/Resources/Sounds` al compilar, y CodeOS los
busca primero ahí, luego en `~/Library/Application Support/Jack/CodeOS/Sounds` y, si no hay ninguno, usa campanas sintetizadas por código.

**Origen:** son sonidos propios, sintetizados con ondas cuadradas y triangulares al estilo de una consola de 8 bits, sin audio de terceros.
Se regeneran con `Scripts/make-codeos-sounds.py` (necesita ffmpeg).
