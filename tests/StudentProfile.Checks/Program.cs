using System.ComponentModel.DataAnnotations;
using Zuni.Models;
using Zuni.Models.MiCuenta;
using Zuni.Helpers;
int checks=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("OK: "+name);}
bool Valid(object model)=>Validator.TryValidateObject(model,new ValidationContext(model),new List<ValidationResult>(),true);
Check(Valid(new DatosEstudianteViewModel()),"MiCuenta permite opcionales vacíos");
foreach(var change in new Action<DatosEstudianteViewModel>[] {m=>m.Telefono="123",m=>m.Telefono="١٢٣٤٥٦٧٨",m=>m.Carrera=new string('a',151),m=>m.Semestre="11",m=>m.CicloAcademico="3",m=>m.TelefonoContactoEmergencia="123",m=>m.NombreContactoEmergencia=new string('a',151),m=>m.RelacionContactoEmergencia=new string('a',61)}){var m=new DatosEstudianteViewModel();change(m);Check(!Valid(m),"MiCuenta rechaza campo inválido");}
Check(Valid(new DatosEstudianteViewModel{Telefono="12345678",Carrera="Ingeniería",Semestre="10",CicloAcademico="2",TelefonoContactoEmergencia="12345678"}),"MiCuenta acepta datos completos");
foreach(var length in new[]{2,3,4,5,6}){var last=new string('1',length);Check(CarneHelper.TryConstruir("7490","20",last,out var carne)&&CarneHelper.Formatear(carne)=="7490-20-"+last,"CarneHelper conserva formato remoto de "+(6+length)+" dígitos");}
foreach(var parts in new[]{("","20","15193"),("7490","","15193"),("7490","20","1"),("7490","20","1234567"),("٧٤٩٠","20","15193")})Check(!CarneHelper.TryConstruir(parts.Item1,parts.Item2,parts.Item3,out _),"Carné inválido rechazado");
var complete=new MiPerfilViewModel{Carne="74902015193",Telefono="12345678",Carrera="Ingeniería"};
Check(complete.PorcentajeAvance==100&&complete.CamposOpcionalesFaltantes.Count==5&&complete.CarneFormateado=="7490-20-15193","Opcionales no reducen progreso y carné 11 se formatea");
Check(new MiPerfilViewModel().PorcentajeAvance==0&&new MiPerfilViewModel{Carne="74902015193"}.PorcentajeAvance==33,"Progreso del perfil vacío y parcial");
Check(!Valid(new EditarMiCuentaViewModel{NombreCompleto="Juan"}),"Editar exige revisión protegida");
Check(!Valid(new EditarMiCuentaViewModel{NombreCompleto="J",Revision="test"}),"Editar rechaza nombre corto");
Check(!Valid(new CambiarContrasenaViewModel{ContrasenaActual="test",NuevaContrasena="ejemplo123",ConfirmarNuevaContrasena="distinta123"}),"Cambio de contraseña exige confirmación coincidente");
var csv=Zuni.Services.EstudiantesCsv.Leer("Nombre,Correo,Carne,Carrera\n\"Alumno, Uno\",uno@miumg.edu.gt,7490-20-15193,Ingeniería\nAlumno Dos,dos@miumg.edu.gt,2020010001,");
Check(csv.Errores.Count==0&&csv.Filas.Count==2&&csv.Filas[0].Carne=="74902015193","CSV conserva comillas y carné de 11");
foreach(var invalid in new[]{"Nombre,Correo,Carne,Carrera\nUno,uno@miumg.edu.gt,2020010001,X\nOtro,UNO@miumg.edu.gt,2020010002,X","Nombre,Correo,Carne,Carrera\nUno,uno@gmail.com,2020010001,X","Nombre,Correo,Carne,Carrera\nUno,uno@miumg.edu.gt,abc,X","Nombre,Correo,Carne,Carrera\n\"Sin cierre","Otra,Cabecera"})Check(Zuni.Services.EstudiantesCsv.Leer(invalid).Errores.Count>0,"CSV inválido rechazado");
foreach(var carne in new[]{"7490-20-11","7490-20-123456"}){
 Check(Valid(new Zuni.Models.Administrador.EditarEstudianteViewModel{Id="test",Nombre="Alumno",Carne=carne}),"Edición administrativa respeta formato de MiCuenta: "+carne);
 Check(Zuni.Services.EstudiantesCsv.Leer("Nombre,Correo,Carne,Carrera\nAlumno,alumno@miumg.edu.gt,"+carne+",Ingeniería").Errores.Count==0,"CSV respeta formato de MiCuenta: "+carne);
}
Console.WriteLine($"APROBADAS: {checks} comprobaciones de MiCuenta, perfil, carné y CSV. Sin conexión a PostgreSQL.");
