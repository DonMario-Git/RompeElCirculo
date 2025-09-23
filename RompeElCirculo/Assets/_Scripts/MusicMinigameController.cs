using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MusicMinigameController : MonoBehaviour
{
    public AudioEffect[] notas;
    public GameObject prefabNota, particulasRomper;

    public int notasPerdidas;

    public List<Nota> listaNotas;

    public RectTransform areaObjetivo; // Asigna esto en el inspector

    public Transform spawnPoint, endPoint;

    public float velocidadNotas;

    public int cantidadNotas = 20;
    private int notascantidad;
    public bool iniciado;
    public bool perdido;
    public bool notasPausadas = false; // Agregado

    private void Awake()
    {
        HelpWindowController.singleton.OnDespause += IniciarMinijuego;
        HelpWindowController.singleton.OnDespause += DespausarNotas;
        HelpWindowController.singleton.OnPause += PausarNotas;
    }

    private void OnEnable()
    {
        iniciado = false;
    }

    public void IniciarMinijuego() 
    {      
        if (!iniciado)
        {   
            notascantidad = cantidadNotas;
            notasPerdidas = 3;
            StartCoroutine(AgregarNotas());        
            perdido = false;
            iniciado = true;
        }      
    }

    IEnumerator AgregarNotas()
    {
        while (notascantidad > 0)
        {
            // Espera si está pausado
            while (notasPausadas)
                yield return null;

            InstanciarNota();
            notascantidad--;
            while (notasPausadas)
                yield return null;
            yield return new WaitForSeconds((Random.Range(3, 5) * 0.5f));
        }
    }

    public void PlayNota(int indice)
    {
        AudioPlayer.singleton.PlayAudioEffect(notas[indice]);
        // Confirmar la nota solo si el idNota coincide con el índice
        if (listaNotas.Any(n => n.idNota == indice))
            ConfirmarNota(indice);
    }

    // Añade esta clase interna para asociar nota y tween
    private class NotaTween
    {
        public RectTransform nota;
        public Tween tween;
    }

    private List<NotaTween> notasConTween = new List<NotaTween>();

    public void InstanciarNota()
    {
        var newNota = Instantiate(prefabNota, spawnPoint.position, Quaternion.identity, spawnPoint.parent).GetComponent<RectTransform>();
        if (newNota == null)
        {
            Debug.LogError("El prefabNota no tiene RectTransform. No se puede agregar la nota.");
            return;
        }

        // Inicia la corrutina para mover la nota
        StartCoroutine(MoverNota(newNota));

        var notaStruct = new Nota { RtrNota = newNota, idNota = Random.Range(0, 8) };
        listaNotas.Add(notaStruct);

        switch (notaStruct.idNota)
        {
            case 0:
                notaStruct.RtrNota.GetComponent<Image>().color = Color.red;
                break;
            case 1:
                notaStruct.RtrNota.GetComponent<Image>().color = new Color(0.7f, 0.21f, 0.83f, 1);
                break;
            case 2:
                notaStruct.RtrNota.GetComponent<Image>().color = Color.blue;
                break;
            case 3:
                notaStruct.RtrNota.GetComponent<Image>().color = Color.cyan;
                break;
            case 4:
                notaStruct.RtrNota.GetComponent<Image>().color = new Color(0, 1, 0.5f, 1);
                break;
            case 5:
                notaStruct.RtrNota.GetComponent<Image>().color = new Color(0.5f, 1, 0, 1);
                break;
            case 6:
                notaStruct.RtrNota.GetComponent<Image>().color = Color.yellow;
                break;
            case 7:
                notaStruct.RtrNota.GetComponent<Image>().color = new Color(1, 0.5f, 0, 1);
                break;
        }
    }

    // Método para pausar el movimiento de las notas
    public void PausarNotas()
    {
        notasPausadas = true;
    }

    // Método para despausar el movimiento de las notas
    public void DespausarNotas()
    {
        notasPausadas = false;
    }

    // Corrutina para mover la nota de forma lineal
    private IEnumerator MoverNota(RectTransform nota)
    {
        if (nota == null) yield break;

        float duracion = 1f / velocidadNotas;
        float tiempo = 0f;
        Vector3 inicio = spawnPoint.position;
        Vector3 fin = endPoint.position;

        while (tiempo < duracion)
        {
            if (nota == null) yield break;

            // Espera si está pausado
            while (notasPausadas)
                yield return null;

            nota.position = Vector3.Lerp(inicio, fin, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (nota == null) yield break;

        nota.position = fin;
        PerderNotas(nota);
    }
    
    public void ConfirmarNota(int idNota)
    {
        GameObject eliminar = null;
        NotaTween notaTweenEliminar = null;
        Nota notaEliminar = default;

        // Obtener las esquinas del área objetivo en espacio mundial
        Vector3[] areaCorners = new Vector3[4];
        areaObjetivo.GetWorldCorners(areaCorners);

        // Calcular el rectángulo del área objetivo en espacio mundial
        float minX = areaCorners[0].x;
        float maxX = areaCorners[2].x;
        float minY = areaCorners[0].y;
        float maxY = areaCorners[2].y;

        foreach (var item in listaNotas)
        {
            if (item.RtrNota != null && item.idNota == idNota)
            {
                Vector3 notaPos = item.RtrNota.position; // posición mundial del centro de la nota

                if (notaPos.x >= minX && notaPos.x <= maxX &&
                    notaPos.y >= minY && notaPos.y <= maxY)
                {
                    eliminar = item.RtrNota.gameObject;
                    notaTweenEliminar = notasConTween.FirstOrDefault(nt => nt.nota == item.RtrNota);
                    notaEliminar = item;
                    break;
                }
            }
        }

        if (eliminar != null)
        {
            listaNotas.Remove(notaEliminar);

            // Detener y eliminar el tween asociado
            if (notaTweenEliminar != null)
            {
                notaTweenEliminar.tween.Kill();
                notasConTween.Remove(notaTweenEliminar);
            }

            // Instanciar particulasRomper en la posición de la nota eliminada con el mismo color
            var notaImage = eliminar.GetComponent<Image>();
            if (notaImage != null && particulasRomper != null)
            {
                var particulasInstance = Instantiate(particulasRomper, eliminar.transform.position, Quaternion.identity, eliminar.transform.parent);
                var particulasImage = particulasInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
                var mainModule = particulasImage.main;
                mainModule.startColor = notaImage.color;
            }

            Destroy(eliminar);

            if (listaNotas.Count == 0 && notascantidad == 0)
            {
                FinalScreenController.instance.Evaluate(true);
            }
        }
    }

    private void PerderNotas(RectTransform nota)
    {
        notasPerdidas--;

        // Remueve la nota de la lista si existe
        var notaItem = listaNotas.FirstOrDefault(n => n.RtrNota == nota);
        if (!notaItem.Equals(default(Nota)))
        {
            listaNotas.Remove(notaItem);
        }

        if (!perdido)
        {
            if (listaNotas.Count == 0 && notascantidad == 0)
            {
                FinalScreenController.instance.Evaluate(true);
            }
        }

        if (notasPerdidas <= 0 && !perdido)
        {
            // Elimina y destruye cada nota correctamente
            foreach (var item in listaNotas.ToList())
            {
                Destroy(item.RtrNota.gameObject);
                listaNotas.Remove(item);
            }

            PausarNotas();

            FinalScreenController.instance.Evaluate(false);
            perdido = true;
        }
    }

    private void OnDestroy()
    {
        HelpWindowController.singleton.OnDespause -= IniciarMinijuego;
    }

    private void OnDrawGizmos()
    {
        if (areaObjetivo != null)
        {
            Vector3[] corners = new Vector3[4];
            areaObjetivo.GetWorldCorners(corners);
            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }
        if (listaNotas != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var item in listaNotas)
            {
                if (item.RtrNota != null)
                {
                    Vector3[] notaCorners = new Vector3[4];
                    item.RtrNota.GetWorldCorners(notaCorners);
                    for (int i = 0; i < 4; i++)
                    {
                        Gizmos.DrawLine(notaCorners[i], notaCorners[(i + 1) % 4]);
                    }
                    // Dibuja el centro de la nota
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(item.RtrNota.position, 0.05f);
                    Gizmos.color = Color.yellow;
                }
            }
        }
    }

    [System.Serializable]
    public struct Nota
    {
        public RectTransform RtrNota;
        public int idNota;
    }
}
