using System;
using System.Collections.Generic;
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

        public static void DesactivarObjeto(this GameObject obj)
        {
            obj.SetActive(false);
        }

        public static void ActivarObjeto(this GameObject obj)
        {
            obj.SetActive(true);
        }

        public static void DesactivarComponente(this MonoBehaviour obj)
        {
            obj.enabled = false;
        }

        public static void ActivarComponente(this MonoBehaviour obj)
        {
            obj.enabled = true;
        }

        public static void DesactivarComponente(this Renderer obj)
        {
            obj.enabled = false;
        }

        public static void ActivarComponente(this Renderer obj)
        {
            obj.enabled = true;
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