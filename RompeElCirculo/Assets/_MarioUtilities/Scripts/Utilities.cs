using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UtilidadesLaEME
{
    public static class Utilities
    {
        /// <summary>
        /// Desordena los elementos de una lista
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void ShuffleList<T>(this IList<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int r = Random.Range(i, n);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }

        public static string ToBase64(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string FromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        public static string GetFirstWord(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var palabras = texto.Trim().Split(' ');
            return palabras.Length > 0 ? palabras[0] : string.Empty;
        }

        public static string DateTimeToString(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yy");
        }

        public static DateTime GetSafeDateTime(DateTime input, bool toUTC = true)
        {
            DateTime result = input;

            if (toUTC)
            {
                if (result.Kind == DateTimeKind.Unspecified)
                    result = DateTime.SpecifyKind(result, DateTimeKind.Local);

                result = result.ToUniversalTime();
            }

            return result;
        }

        /// <summary>
        /// Trunca un texto al número de palabras indicado
        /// y añade puntos suspensivos si fue recortado.
        /// </summary>
        public static string TruncateByWords(
            this string text,
            int maxWords,
            string ellipsis = "...")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (maxWords <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(maxWords),
                    "maxWords debe ser mayor que cero.");

            var words = text.Split(
                new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= maxWords)
                return text;

            return string.Join(" ", words, 0, maxWords) + ellipsis;
        }

        public static DateTime GetSafeDateTime(string input, bool toUTC = true)
        {
            string[] formatos =
            {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "dd/MM/yyyy",
            "MM/dd/yyyy"
        };

            if (!DateTime.TryParseExact(
                input,
                formatos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime result))
            {
                throw new Exception("Formato de fecha inválido: " + input);
            }

            // Detecci?n b?sica de ambig?edad
            if (input.Contains("-"))
            {
                var partes = input.Split('-');
                if (partes.Length >= 2 &&
                    int.TryParse(partes[0], out int a) &&
                    int.TryParse(partes[1], out int b))
                {
                    if (a <= 12 && b <= 12)
                    {
                        Debug.LogWarning("Fecha potencialmente ambigua: " + input);
                    }
                }
            }

            if (toUTC)
            {
                if (result.Kind == DateTimeKind.Unspecified)
                    result = DateTime.SpecifyKind(result, DateTimeKind.Local);

                result = result.ToUniversalTime();
            }

            return result;
        }

        public static DateTime StringToDateTime(string fecha)
        {
            try
            {
                return DateTime.ParseExact(fecha, "dd/MM/yy", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                throw new FormatException($"El string '{fecha}' no tiene el formato esperado dd/MM/yy.");
            }
        }

        public static DateTime UTCToLocal(DateTime utcTime)
        {
            // Asegurar que el DateTime sea UTC
            if (utcTime.Kind == DateTimeKind.Unspecified)
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            if (utcTime.Kind == DateTimeKind.Local)
                return utcTime; // ya es local

            return utcTime.ToLocalTime();
        }

        public static DateTime CrearFecha(int dia, int mes, int año)
        {
            return new DateTime(año, mes, dia);
        }

        /// <summary>
        /// Obtiene el ID ?nico del dispositivo
        /// </summary>
        public static string GetDeviceID()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        /// <summary>
        /// Removes whitespace characters at the start and end of the string.
        /// </summary>
        public static string TrimEdges(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Trim();
        }

        /// <summary>
        /// Verifica si un string empieza con un caracter especifico.
        /// </summary>
        public static bool StartsWithChar(this string text, char character)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text[0] == character;
        }

        /// <summary>
        /// Verifica si un string termina con un caracter especifico.
        /// </summary>
        public static bool EndsWithChar(this string text, char character)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text[^1] == character; // ^1 = ultimo caracter
        }

        /// <summary>
        /// Removes extra spaces inside the string, keeping only one space between words.
        /// </summary>
        public static string NormalizeInnerSpaces(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Trim();
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }
            return text;
        }

        /// <summary>
        /// Removes all extra whitespace (spaces, tabs, new lines) in the string, leaving only single spaces.
        /// </summary>
        public static string NormalizeWhitespace(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim();
            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        public static string CorregirAcentos(this string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            return texto
                .Replace('á', 'a')
                .Replace('à', 'a')
                .Replace('ä', 'a')
                .Replace('â', 'a')
                .Replace('Á', 'A')
                .Replace('À', 'A')
                .Replace('Ä', 'A')
                .Replace('Â', 'A')

                .Replace('é', 'e')
                .Replace('è', 'e')
                .Replace('ë', 'e')
                .Replace('ê', 'e')
                .Replace('É', 'E')
                .Replace('È', 'E')
                .Replace('Ë', 'E')
                .Replace('Ê', 'E')

                .Replace('í', 'i')
                .Replace('ì', 'i')
                .Replace('ï', 'i')
                .Replace('î', 'i')
                .Replace('Í', 'I')
                .Replace('Ì', 'I')
                .Replace('Ï', 'I')
                .Replace('Î', 'I')

                .Replace('ó', 'o')
                .Replace('ò', 'o')
                .Replace('ö', 'o')
                .Replace('ô', 'o')
                .Replace('Ó', 'O')
                .Replace('Ò', 'O')
                .Replace('Ö', 'O')
                .Replace('Ô', 'O')

                .Replace('ú', 'u')
                .Replace('ù', 'u')
                .Replace('ü', 'u')
                .Replace('û', 'u')
                .Replace('Ú', 'U')
                .Replace('Ù', 'U')
                .Replace('Ü', 'U')
                .Replace('Û', 'U');
        }

        public static void DesactivarObjeto(this GameObject obj)
        {
            if (obj != null) obj.SetActive(false);
        }

        public static void ActivarObjeto(this GameObject obj)
        {
            if (obj != null) obj.SetActive(true);
        }

        public static void DesactivarComponente(this MonoBehaviour obj)
        {
            if (obj != null) obj.enabled = false;
        }

        public static void ActivarComponente(this MonoBehaviour obj)
        {
            if (obj != null) obj.enabled = true;
        }

        public static void DesactivarComponente(this Renderer obj)
        {
            if (obj != null) obj.enabled = false;
        }

        public static void ActivarComponente(this Renderer obj)
        {
            if (obj != null) obj.enabled = true;
        }

        /// <summary>
        /// Valida que tan confiable es un Email
        /// </summary>
        public static bool EsUnEmailValido(this string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        /// <summary>
        /// Valida la seguridad de una contraseña y retorna un mensaje de recomendaci?n
        /// </summary>
        public static bool ValidarCaracteresContraseña(this string password, out string result)
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

        /// <summary>
        /// Calcula la edad en años entre dos fechas (fechaNacimiento y fechaActual).
        /// </summary>
        public static int CalcularEdad(DateTime fechaNacimiento, DateTime fechaActual)
        {
            int edad = fechaActual.Year - fechaNacimiento.Year;
            if (fechaActual < fechaNacimiento.AddYears(edad))
                edad--;
            return edad;
        }

        public static void AddComponentDelayed<T>(GameObject target, System.Action<T> onCreated = null)
        where T : Component
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (target == null)
                        return;

                    T component = target.GetComponent<T>();

                    if (component == null)
                        component = target.AddComponent<T>();

                    onCreated?.Invoke(component);
                };

                return;
            }
#endif

            T runtimeComponent = target.GetComponent<T>();

            if (runtimeComponent == null)
                runtimeComponent = target.AddComponent<T>();

            onCreated?.Invoke(runtimeComponent);
        }

        public static void RemoveComponentDelayed<T>(ref T component)
            where T : Component
        {
            if (component == null)
                return;

            T componentToRemove = component;
            component = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (componentToRemove != null)
                        Object.DestroyImmediate(componentToRemove);
                };

                return;
            }
#endif

            Object.Destroy(componentToRemove);
        }
    }

    public enum Direccion
    {
        IZQUIERDA, DERECHA
    }

    public interface ICampoObligatorioComprobacion
    {
        public bool EstaContestado();
        public void ToggleObligatorio();
    }
}