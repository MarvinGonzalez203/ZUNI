namespace Zuni.Resources;

public static class AccesosGuias
{
    public static string Enlace(GuiaUso g,string rol)
    {
        var pregunta=GuiasUso.Preguntas.FirstOrDefault(p=>p.Titulo==g.Titulo);
        if(pregunta is not null)return pregunta.Enlace;
        return g.Titulo switch {
            "Consultar quién asigna mi psicólogo"=>"/Estudiante/EstadoSolicitud",
            "Activar o desactivar el modo noche" or "Elegir tema y colores"=>"#zuni-theme-control",
            "Cambiar español e inglés"=>"#zuni-language-control",
            "Comprimir y ampliar el menú lateral"=>"#zuni-menu-control",
            "Leer una ventana con más información" or "Explorar contenido largo"=>"#zuni-scroll-control",
            "Leer novedades de ZUNI" or "Reconocer la versión instalada"=>"#zuni-release-control",
            "Consultar notificaciones de tu actividad"=>rol=="Director"?"/Director/Propuestas":rol=="Psicologo"?"/Psicologo/Agenda":rol=="Estudiante"?"/CitasProgramadas/Notificaciones":"#zuni-release-control",
            "Buscar ayuda para tu perfil"=>"/Guias",
            "Corregir los datos de un estudiante"=>"/Administrador/Estudiantes",
            "Mantener el catálogo de carreras"=>"/CarrerasAdmin",
            "Configurar la institución"=>"/Institucion",
            "Publicar la política institucional"=>"/Privacidad/Configurar",
            "Responder solicitudes sobre datos"=>"/Privacidad/Solicitudes",
            "Consultar la verificación de mayoría de edad"=>"/MayoriaEdad",
            "Entender por qué aún no puedes reservar"=>"/Estudiante/EstadoSolicitud",
            "Cancelar una cita futura"=>"/Estudiante/Citas",
            "Revisar una propuesta del Director"=>"/Psicologo/Agenda",
            "Seguir el estado de una propuesta enviada"=>"/Director/Propuestas",
            "Revisar vigencias y contratos próximos a vencer" or "Ampliar o renovar un contrato" or "Suspender o cancelar un contrato"=>"/SuperAdmin/Centros",
            "Actualizar las tarifas"=>"/Tarifas",
            "Editar datos de la cuenta"=>"/MiCuenta",
            "Preparar una reunión virtual"=>"/Psicologo/Agenda",
            "Registrar atención imprevista"=>"/Atenciones/Imprevista",
            "Indicar un cuestionario con inducción"=>"/PruebasAplicadas/Habilitar",
            "Preparar un informe educativo"=>"/InformesEducativos",
            "Preparar el catálogo de cuestionarios"=>"/PruebasDirector",
            "Responder un cuestionario indicado"=>"/PruebasAplicadas",
            "Autorizar o revocar un informe"=>"/InformesEducativos",
            "Leer políticas y ejercer derechos"=>"/Privacidad",
            "Proteger tu sesión"=>"#zuni-logout-control",
            "Preparar un espacio privado"=>rol=="Psicologo"?"/Psicologo/Agenda":rol=="Estudiante"?"/Estudiante/Citas":"/MiCuenta",
            "Cuidar los documentos descargados"=>rol=="Estudiante"?"/Estudiante/Resultados":rol=="SuperAdmin"?"/SuperAdmin/Centros":"/InformesEducativos",
            _=>throw new InvalidOperationException("La guía debe tener un acceso definido: "+g.Titulo)
        };
    }
}
