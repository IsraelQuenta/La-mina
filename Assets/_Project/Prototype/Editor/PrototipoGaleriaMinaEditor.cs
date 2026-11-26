using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototipoGaleriaMinaEditor
{
    private const string MenuItemPath = "Herramientas/El Último Turno/Crear Prototipo Galería Mina";
    private const string RootFolder = "Assets/_Project";
    private const string SceneFolder = RootFolder + "/Scenes";
    private const string PrototypeFolder = RootFolder + "/Prototype";
    private const string EditorFolder = PrototypeFolder + "/Editor";
    private const string MaterialsFolder = PrototypeFolder + "/Materials";
    private const string ScenePath = SceneFolder + "/Prototipo_GaleriaMina.unity";
    private const string SueloPath = MaterialsFolder + "/M_Suelo_Mina.mat";
    private const string RocaPath = MaterialsFolder + "/M_Roca_Mina.mat";
    private const string MaderaPath = MaterialsFolder + "/M_Madera_Mina.mat";

    [MenuItem(MenuItemPath)]
    private static void CrearPrototipo()
    {
        var creadosEnEstaEjecucion = new List<string>();
        bool escenaCreadaEnMemoria = false;

        try
        {
            if (!Preflight())
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[El Último Turno] Generación cancelada por el usuario. No se crearon ni modificaron activos.");
                return;
            }

            CrearCarpetaSiFalta(RootFolder, creadosEnEstaEjecucion);
            CrearCarpetaSiFalta(SceneFolder, creadosEnEstaEjecucion);
            CrearCarpetaSiFalta(PrototypeFolder, creadosEnEstaEjecucion);
            CrearCarpetaSiFalta(EditorFolder, creadosEnEstaEjecucion);
            CrearCarpetaSiFalta(MaterialsFolder, creadosEnEstaEjecucion);

            Material suelo = CrearMaterial(SueloPath, new Color(0.12f, 0.12f, 0.13f, 1f), creadosEnEstaEjecucion);
            Material roca = CrearMaterial(RocaPath, new Color(0.18f, 0.18f, 0.20f, 1f), creadosEnEstaEjecucion);
            Material madera = CrearMaterial(MaderaPath, new Color(0.30f, 0.18f, 0.09f, 1f), creadosEnEstaEjecucion);

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            escenaCreadaEnMemoria = true;
            ConstruirEscena(suelo, roca, madera);

            creadosEnEstaEjecucion.Add(ScenePath);
            if (!EditorSceneManager.SaveScene(escena, ScenePath))
            {
                throw new InvalidOperationException("Unity no pudo guardar la escena en " + ScenePath);
            }

            Debug.Log("[El Último Turno] Prototipo creado correctamente: " + ScenePath);
        }
        catch (Exception ex)
        {
            if (escenaCreadaEnMemoria)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            Limpiar(creadosEnEstaEjecucion);
            Debug.LogError("[El Último Turno] Error al generar el prototipo: " + ex.Message + " Se limpiaron únicamente los elementos creados en esta ejecución.");
        }
    }

    private static bool Preflight()
    {
        if (ExisteAsset(ScenePath) || ExisteAsset(SueloPath) || ExisteAsset(RocaPath) || ExisteAsset(MaderaPath))
        {
            Debug.LogError("[El Último Turno] Ya existe la escena o uno de los materiales destino. No se sobrescribirá nada.");
            return false;
        }

        if (Shader.Find("Universal Render Pipeline/Lit") == null)
        {
            Debug.LogError("[El Último Turno] No se encontró el shader Universal Render Pipeline/Lit. No se creó ningún activo.");
            return false;
        }

        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene escena = EditorSceneManager.GetSceneAt(i);
            if (escena.isDirty && !string.IsNullOrEmpty(escena.path))
            {
                Debug.LogError("[El Último Turno] Hay una escena existente con cambios sin guardar: " + escena.path + ". Guárdala o reviértela manualmente antes de generar el prototipo.");
                return false;
            }
        }

        return true;
    }

    private static bool ExisteAsset(string path)
    {
        return AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(Path.GetFullPath(path));
    }

    private static void CrearCarpetaSiFalta(string path, List<string> creados)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string padre = Path.GetDirectoryName(path).Replace('\\', '/');
        string nombre = Path.GetFileName(path);

        if (string.IsNullOrEmpty(padre) || !AssetDatabase.IsValidFolder(padre))
        {
            throw new InvalidOperationException("No existe la carpeta padre requerida: " + padre);
        }

        string guid = AssetDatabase.CreateFolder(padre, nombre);
        if (string.IsNullOrEmpty(guid))
        {
            throw new InvalidOperationException("No se pudo crear la carpeta: " + path);
        }

        creados.Add(path);
    }

    private static Material CrearMaterial(string path, Color color, List<string> creados)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontró el shader Universal Render Pipeline/Lit.");
        }

        var material = new Material(shader)
        {
            name = Path.GetFileNameWithoutExtension(path)
        };
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0f);

        AssetDatabase.CreateAsset(material, path);
        creados.Add(path);
        return material;
    }

    private static void ConstruirEscena(Material suelo, Material roca, Material madera)
    {
        Transform raiz = CrearVacio("Prototipo_GaleriaMina", null, Vector3.zero);
        Transform geometria = CrearVacio("Geometria", raiz, Vector3.zero);
        Transform soportes = CrearVacio("Soportes", raiz, Vector3.zero);
        Transform rocas = CrearVacio("Rocas", raiz, Vector3.zero);
        Transform iluminacion = CrearVacio("Iluminacion", raiz, Vector3.zero);
        Transform camara = CrearVacio("Camara", raiz, Vector3.zero);

        CrearBloque("Suelo", geometria, new Vector3(0f, -0.1f, 0f), new Vector3(4f, 0.2f, 20f), Vector3.zero, suelo);
        CrearBloque("Pared_Izquierda", geometria, new Vector3(-2.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f), Vector3.zero, roca);
        CrearBloque("Pared_Derecha", geometria, new Vector3(2.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f), Vector3.zero, roca);
        CrearBloque("Techo", geometria, new Vector3(0f, 3.1f, 0f), new Vector3(4f, 0.2f, 20f), Vector3.zero, roca);

        CrearSoporte("Soporte_01", soportes, -6f, madera);
        CrearSoporte("Soporte_02", soportes, 6f, madera);

        CrearBloque("Roca_01", rocas, new Vector3(-1.6f, 0.18f, -8f), new Vector3(0.6f, 0.36f, 0.7f), new Vector3(0f, 15f, 5f), roca);
        CrearRoca("Roca_02", rocas, new Vector3(1.6f, 0.18f, -3f), new Vector3(0.7f, 0.36f, 0.7f), Vector3.zero, roca);
        CrearBloque("Roca_03", rocas, new Vector3(-1.6f, 0.22f, 2f), new Vector3(0.55f, 0.44f, 0.7f), new Vector3(8f, 20f, 0f), roca);
        CrearRoca("Roca_04", rocas, new Vector3(1.55f, 0.16f, 8f), new Vector3(0.75f, 0.32f, 0.6f), Vector3.zero, roca);

        CrearLuz("Luz_Tunel_01", iluminacion, new Vector3(0f, 2.6f, -5f), new Color(1f, 0.62f, 0.36f, 1f), 1.0f);
        CrearLuz("Luz_Tunel_02", iluminacion, new Vector3(0f, 2.6f, 5f), new Color(1f, 0.70f, 0.45f, 1f), 0.8f);

        CrearCamara("Camara_Preview", camara);
    }

    private static Transform CrearVacio(string nombre, Transform padre, Vector3 posicionLocal)
    {
        var go = new GameObject(nombre);
        if (padre != null)
        {
            go.transform.SetParent(padre, false);
        }
        go.transform.localPosition = posicionLocal;
        return go.transform;
    }

    private static GameObject CrearBloque(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Vector3 rotacion, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        go.transform.localEulerAngles = rotacion;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject CrearRoca(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Vector3 rotacion, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        go.transform.localEulerAngles = rotacion;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static void CrearSoporte(string nombre, Transform padre, float z, Material madera)
    {
        Transform soporte = CrearVacio(nombre, padre, new Vector3(0f, 0f, z));
        CrearBloque("Poste_Izquierdo", soporte, new Vector3(-1.9f, 1.35f, 0f), new Vector3(0.18f, 2.7f, 0.18f), Vector3.zero, madera);
        CrearBloque("Poste_Derecho", soporte, new Vector3(1.9f, 1.35f, 0f), new Vector3(0.18f, 2.7f, 0.18f), Vector3.zero, madera);
        CrearBloque("Viga", soporte, new Vector3(0f, 2.85f, 0f), new Vector3(3.9f, 0.18f, 0.18f), Vector3.zero, madera);
    }

    private static void CrearLuz(string nombre, Transform padre, Vector3 posicion, Color color, float intensidad)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;

        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = 6f;
        luz.shadows = LightShadows.Soft;
    }

    private static void CrearCamara(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        go.tag = "MainCamera";
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 1.5f, -9f);
        go.transform.localEulerAngles = Vector3.zero;

        Camera camara = go.AddComponent<Camera>();
        camara.fieldOfView = 60f;
        camara.nearClipPlane = 0.05f;
        camara.farClipPlane = 30f;
        go.AddComponent<AudioListener>();
    }

    private static void Limpiar(List<string> creados)
    {
        for (int i = creados.Count - 1; i >= 0; i--)
        {
            string path = creados[i];

            try
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    bool vacia = AssetDatabase.FindAssets(string.Empty, new[] { path }).Length == 0 &&
                                 Directory.GetFileSystemEntries(Path.GetFullPath(path)).Length == 0;
                    if (!vacia)
                    {
                        Debug.LogWarning("[El Último Turno] No se eliminó la carpeta porque contiene archivos: " + path);
                        continue;
                    }

                    if (!AssetDatabase.DeleteAsset(path))
                    {
                        Debug.LogWarning("[El Último Turno] No se pudo eliminar la carpeta creada: " + path);
                    }
                    continue;
                }

                if (ExisteAsset(path) && !AssetDatabase.DeleteAsset(path))
                {
                    Debug.LogWarning("[El Último Turno] No se pudo eliminar el activo creado: " + path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[El Último Turno] No se pudo limpiar " + path + ": " + ex.Message);
            }
        }
    }
}
