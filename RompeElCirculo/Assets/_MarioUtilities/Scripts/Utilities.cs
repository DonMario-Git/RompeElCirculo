using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UtilidadesLaEME
{
    public static class Utilities
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int r = Random.Range(i, n);
                (list[i], list[r]) = (list[r], list[i]);
            }
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