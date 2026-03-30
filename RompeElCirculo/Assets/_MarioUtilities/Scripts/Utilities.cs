using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
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

            // Detección básica de ambigüedad
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
        /// Obtiene el ID único del dispositivo
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
        /// Verifica si un string empieza con un carácter específico.
        /// </summary>
        public static bool StartsWithChar(this string text, char character)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text[0] == character;
        }

        /// <summary>
        /// Verifica si un string termina con un carácter específico.
        /// </summary>
        public static bool EndsWithChar(this string text, char character)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text[^1] == character; // ^1 = último carácter
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
            if (string.IsNullOrEmpty(texto)) return texto;

            return texto
                .Replace("à", "á")
                .Replace("è", "é")
                .Replace("ì", "í")
                .Replace("ò", "ó")
                .Replace("ù", "ú")
                .Replace("À", "Á")
                .Replace("È", "É")
                .Replace("Ì", "Í")
                .Replace("Ò", "Ó")
                .Replace("Ù", "Ú");
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
        /// Valida la seguridad de una contraseña y retorna un mensaje de recomendación
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