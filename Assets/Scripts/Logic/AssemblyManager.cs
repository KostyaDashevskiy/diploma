using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AssemblyManager : MonoBehaviour
{
    public static AssemblyManager Instance { get; private set; }

    [Header("Облачная база (GitHub RAW URL)")]
    [Tooltip("Ссылка на твой GlobalPartsDB.json (RAW)")]
    public string serverJsonUrl = "";

    [Header("Локальная резервная база")]
    [Tooltip("Перекрывает данные с сервера! Удобно для тестов и модов")]
    public TextAsset localCustomJson; 

    public Dictionary<string, PartData> database = new Dictionary<string, PartData>();

    [Header("Текущая сборка")]
    public List<PartData> installedParts = new List<PartData>();
    public float currentPowerSupplyCapacity = 0f; 
    public float currentTotalTDP = 0f;            

    private string cacheFilePath; 
    public bool isDatabaseReady = false; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Путь для сохранения облачной базы на ПК (AppData/LocalLow/...)
        cacheFilePath = Path.Combine(Application.persistentDataPath, "GlobalPartsDB_Cache.json");
    }

    private void Start()
    {
        // Запускаем единую очередь загрузки
        StartCoroutine(LoadDatabaseRoutine());
    }

    // --- ЕДИНАЯ ОЧЕРЕДЬ ЗАГРУЗКИ ---
    private IEnumerator LoadDatabaseRoutine()
    {
        bool cloudSuccess = false;

        // ШАГ 1: Скачиваем свежую версию с GitHub
        if (!string.IsNullOrEmpty(serverJsonUrl))
        {
            string noCacheUrl = serverJsonUrl + "?t=" + Random.Range(1000, 99999);
            using (UnityWebRequest request = UnityWebRequest.Get(noCacheUrl))
            {
                // Игнорируем ошибку "Unable to complete SSL connection"
                request.certificateHandler = new BypassCertificate();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string downloadedJson = request.downloadHandler.text;
                    File.WriteAllText(cacheFilePath, downloadedJson);

                    database.Clear(); 
                    ParseAndAddToDict(downloadedJson, "GitHub (Свежая версия)");
                    cloudSuccess = true;
                }
                else
                {
                    Debug.LogWarning($"<color=red>[Облако]</color> Ошибка скачивания базы: {request.error}.");
                }
            }
        }

        // ШАГ 2: Если интернет не сработал - грузим Кэш
        if (!cloudSuccess && File.Exists(cacheFilePath))
        {
            database.Clear();
            ParseAndAddToDict(File.ReadAllText(cacheFilePath), "Локальный Кэш");
        }

        // ШАГ 3: ЛОКАЛЬНЫЙ ПОЛЬЗОВАТЕЛЬСКИЙ ФАЙЛ (Главный приоритет!)
        // Этот файл прочитается в самом конце. Если в нем есть изменения, они заменят серверные!
        if (localCustomJson != null && !string.IsNullOrEmpty(localCustomJson.text))
        {
            ParseAndAddToDict(localCustomJson.text, "Локальный файл (Моды/Тесты)");
        }

        // Говорим Магазину, что база полностью загружена и можно рисовать карточки
        isDatabaseReady = true; 
    }

    private void ParseAndAddToDict(string jsonText, string sourceName)
    {
        if (string.IsNullOrEmpty(jsonText)) return;
        try
        {
            PartDatabase db = JsonUtility.FromJson<PartDatabase>(jsonText);
            foreach (PartData part in db.parts)
            {
                // Жестко перезаписываем данные: локальный файл перекроет облачный
                database[part.partID] = part;
            }
            Debug.Log($"<color=green>[База Данных]</color> Обработан источник: {sourceName}. Деталей в базе: {database.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[База Данных] Ошибка парсинга JSON: {e.Message}");
        }
    }

    // --- ОСТАЛЬНАЯ ЛОГИКА ---

    public PartData GetPartInfo(string searchID)
    {
        if (database.ContainsKey(searchID)) return database[searchID];
        return null;
    }

    public void AddPartToAssembly(PartData newPart, ItemType type)
    {
        installedParts.Add(newPart);
        if (type == ItemType.PowerSupply) currentPowerSupplyCapacity = newPart.tdp;
        else currentTotalTDP += newPart.tdp;
        CheckPowerBalance();
    }

    public void RemovePartFromAssembly(PartData removedPart, ItemType type)
    {
        installedParts.Remove(removedPart);
        if (type == ItemType.PowerSupply) currentPowerSupplyCapacity = 0f;
        else currentTotalTDP -= removedPart.tdp;
        CheckPowerBalance();
    }

    public bool CheckPowerBalance()
    {
        if (installedParts.Count == 0) return true;
        float requiredPeakPower = currentTotalTDP * 1.2f;

        if (currentPowerSupplyCapacity >= requiredPeakPower) return true;
        else return false;
    }
}

// === КЛАСС ДЛЯ ИГНОРИРОВАНИЯ ОШИБОК SSL ===
public class BypassCertificate : UnityEngine.Networking.CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; 
    }
}