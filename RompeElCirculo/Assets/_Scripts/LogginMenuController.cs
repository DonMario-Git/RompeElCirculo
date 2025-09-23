using DG.Tweening;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilidadesLaEME;

public class LogginMenuController : MonoBehaviour
{
    public static LogginMenuController singleton;

    [Header("Loggin")]
    public TMP_InputField input_Nombres2;
    public TMP_InputField input_Contraseña;
    public TextMeshProUGUI tmp_Resultado;

    [Header("Registrar")]

    public TMP_InputField input_Nombres;
    public TMP_InputField input_Apellidos;
    public TMP_InputField input_Correo2;
    public TMP_InputField input_Contraseña2;
    public TMP_InputField input_ConfirmarContraseña;
    public TMP_InputField input_Documento;

    public Image imagenFrente;

    public GameObject formulario;
    public AnimacionAlActivar letras;

    private void Awake()
    {
        singleton = this;
    }

    public void InicializarLoggin()
    {
        transform.GetChild(0).gameObject.SetActive(true);
        imagenFrente.gameObject.SetActive(true);
        imagenFrente.DOFade(0, 0.3f).OnComplete(() => imagenFrente.raycastTarget = false);
    }

    public void RegistrarCuenta()
    {
        if (IsValidEmail(input_Correo2.text))
        {
            DesactivarResultadoTexto();

            if (!string.IsNullOrEmpty(input_Nombres.text))
            {
                if (!string.IsNullOrEmpty(input_Apellidos.text))
                {
                    if (input_Contraseña2.text == input_ConfirmarContraseña.text)
                    {
                        if (!string.IsNullOrEmpty(input_Documento.text))
                        {
                            if (ValidarCaracteresContraseña(input_Contraseña2.text, out string result))
                            {
                                RegistrarCuentaFireBase();
                            }
                            else
                            {
                                ColocarResultadoTexto(result, Color.red);
                            }
                        }
                        else
                        {
                            ColocarResultadoTexto("Introduzca un numero de documento válido", Color.red);
                        }
                    }
                    else
                    {
                        ColocarResultadoTexto("Las contraseñas no coinciden", Color.red);
                    }   
                }
                else
                {
                    ColocarResultadoTexto("Introduzca un apellido", Color.red);
                }
            }
            else
            {
                ColocarResultadoTexto("Introduzca un nombre", Color.red);
            }                
        }
        else
        {
            ColocarResultadoTexto("Debe colocar un correo válido", Color.red);
        }

        void RegistrarCuentaFireBase()
        {
            Data nuevosDatos = new()
            {
                Nombres = input_Nombres.text.TrimEdges(),
                Apellidos = input_Apellidos.text.TrimEdges(),
                CorreoElecctronico = input_Correo2.text.TrimEdges(),
                Contrasenia = input_Contraseña2.text.TrimEdges(),
                numeroDocumento = int.Parse(input_Documento.text.TrimEdges()),
                ultimoDispositivo = AppManager.GetDeviceID(),
                dispositivoCreacion = AppManager.GetDeviceID()
            };

            FirebaseStorageManager.singleton.SaveData(nuevosDatos, input_Nombres.text.TrimEdges(), false, (resultado) => {

                if (string.IsNullOrEmpty(resultado))
                {
                    ColocarResultadoTexto("Cuenta registrada", Color.green);
                    SerialCheckerController.singleton.GuardarLicencia(int.Parse(input_Documento.text.TrimEdges()), DateTime.UtcNow, input_Documento.text.TrimEdges(), (result) => {
                        if (!string.IsNullOrEmpty(result)) Debug.LogError(result);
                    });
                    return;
                }
                else
                {
                    ColocarResultadoTexto(resultado, Color.red);
                } 
            });
            
        }
    }  

    public void IniciarSesion()
    {
        if (!string.IsNullOrEmpty(input_Nombres2.text))
        {
            DesactivarResultadoTexto();

            if (ValidarCaracteresContraseña(input_Contraseña.text.TrimEdges(), out string result))
            {
                ValidarLogginConFireBase();
            }
            else
            {
                ColocarResultadoTexto(result, Color.red);
            }
        }
        else
        {
            ColocarResultadoTexto("Debe colocar un nombre válido", Color.red);
        }

        void ValidarLogginConFireBase()
        {
            Data datosTemporales = null;

            // Adjust the lambda to match the expected delegate signature (Action<Data, string>)
            FirebaseStorageManager.singleton.LoadData(input_Nombres2.text.TrimEdges(), (datos, error) =>
            {
                if (datos != null)
                {
                    datosTemporales = datos;

                    if (datosTemporales.Contrasenia == input_Contraseña.text.TrimEdges())
                    {
                        ColocarResultadoTexto("Inicio de sesión exitoso", Color.green);

                        SerialCheckerController.singleton.CargarYCompararLicencia(datos.numeroDocumento, (vencida, licencia, diasFaltantes, error2) => {

                        if (!string.IsNullOrEmpty(error2))
                            datos.activo = !vencida;    
                        else 
                            datos.activo = false;

                            AppManager.diasFaltantes = diasFaltantes;
                            Debug.LogWarning(error2);
                        });

                        AppManager.data = datos;

                        AppManager.userData = new UserData()
                        {
                            Nombres = datos.Nombres,
                            Contrasenia = datos.Contrasenia,
                            ultimoDispositivo = AppManager.GetDeviceID()
                        };

                        IntroController.singleton.GuardarDatosDispositivo();
                        FirebaseStorageManager.singleton.SaveData(AppManager.data, AppManager.data.Nombres, true, (resultado) => {
                            if (!string.IsNullOrEmpty(resultado)) Debug.LogWarning(resultado);
                                }, false);

                        IEnumerator DelayIniciar()
                        {
                            yield return new WaitForSeconds(0.4f);

                            imagenFrente.raycastTarget = true;
                            imagenFrente.DOKill();
                            imagenFrente.DOFade(1, 0.3f).OnComplete(() => { 
                                imagenFrente.gameObject.SetActive(false);
                                transform.GetChild(0).gameObject.SetActive(false);
                                tmp_Resultado.gameObject.SetActive(false);
                            });

                            yield return new WaitForSeconds(1f);

                            if (AppManager.data.hizoFormulario)
                            {
                                IntroController.singleton.IntroAnimation();
                            }
                            else
                            {
                                letras.gameObject.SetActive(true);

                                StartCoroutine(Secuencia());

                                IEnumerator Secuencia()
                                {
                                    yield return new WaitForSeconds(1.5f);

                                    letras.DesactivarTexto();
                                    yield return new WaitForSeconds(0.5f);

                                    formulario.SetActive(true);
                                }
                            }
                        }

                        StartCoroutine(DelayIniciar());
                    }
                    else
                    {
                        ColocarResultadoTexto("Nombre o contraseña incorrectos", Color.red);
                    }
                }
                else
                {
                    ColocarResultadoTexto($"Error al cargar datos: {error}", Color.red);
                }
            });
        }
    }

    private void ColocarResultadoTexto(string texto, Color color)
    {
        tmp_Resultado.color = color;
        tmp_Resultado.text = texto;
        tmp_Resultado.transform.localScale = Vector3.one * 1.2f;
        tmp_Resultado.gameObject.SetActive(true);
        tmp_Resultado.transform.DOKill();
        tmp_Resultado.transform.DOScale(1, 0.3f);
    }

    private void DesactivarResultadoTexto()
    {
        tmp_Resultado.transform.DOKill();
        tmp_Resultado.gameObject.SetActive(false);
    }

    private bool IsValidEmail(string email)
    {
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    private bool ValidarCaracteresContraseña(string password, out string result)
    {
        if (password.Length < 8)
        {
            result = "La contraseña debe tener al menos 8 caracteres.";
            return false;
        }      

        if (password.Contains(" "))
        {
            result = "La contraseña no puede contener espacios.";
            return false;
        }

        result = string.Empty;
        return true; // Todo bien
    }
}
