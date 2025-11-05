using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I;
    public DataRegistry registry; // drag Assets/Data/RevCivRegistry.asset here

    void Awake()
    {
        if (I != null && I != this) Destroy(gameObject);
        else I = this;
    }

    [System.Serializable]
    public class TileDTO
    {
        public Vector3 pos;
        public string biome;
        public string subBiome;
        public float temperature;
        public float moisture;
        public int ownerSettlementId;
        public bool worked;
        public string buildingId;
        public List<string> resources;
    }

    [System.Serializable]
    public class SettlementDTO
    {
        public int id;
        public string name;
        public int population;
        public Vector3 pos;
        public List<string> stock;
    }

    [System.Serializable]
    public class UnitDTO
    {
        public string unitId;
        public Vector3 pos;
    }

    [System.Serializable]
    public class SaveGame
    {
        public int saveVersion = 1;
        public int dayCount = 0;
        public List<TileDTO> tiles = new();
        public List<SettlementDTO> settlements = new();
        public List<UnitDTO> units = new();
    }

    int dayCounter = 0;

    void OnEnable() { TimeController.OnDailyTick += OnDay; }
    void OnDisable() { TimeController.OnDailyTick -= OnDay; }

    void OnDay() => dayCounter++;

    string Dir => Path.Combine(Application.persistentDataPath, "RevCiv", "Saves");
    string PathFor(string slot) => System.IO.Path.Combine(Dir, slot + ".json");

    public void Save(string slot)
    {
        if (!registry)
        {
            Debug.LogError("SaveManager: registry not assigned!");
            return;
        }

        Directory.CreateDirectory(Dir);
        var sg = new SaveGame { dayCount = dayCounter };

        var tiles = Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
        foreach (var t in tiles)
        {
            var td = new TileDTO
            {
                pos = t.transform.position,
                biome = t.biome,
                subBiome = t.subBiome,
                temperature = t.temperature,
                moisture = t.moisture,
                ownerSettlementId = t.ownerSettlementId,
                worked = t.worked,
                buildingId = t.buildingInstance ? t.buildingInstance.GetComponent<BuildingInstance>().def?.id : null,
                resources = new List<string>()
            };
            foreach (var rs in t.resources)
                if (rs.resource)
                    td.resources.Add($"{rs.resource.id}:{rs.amountPerDay}");
            sg.tiles.Add(td);
        }

        var settlements = Object.FindObjectsByType<Settlement>(FindObjectsSortMode.None);
        foreach (var s in settlements)
        {
            var sd = new SettlementDTO
            {
                id = s.settlementId,
                name = s.settlementName,
                population = s.population,
                pos = s.transform.position,
                stock = new List<string>()
            };
            foreach (var st in s.stockpile)
                if (st.resource)
                    sd.stock.Add($"{st.resource.id}:{st.amount}");
            sg.settlements.Add(sd);
        }

        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var u in units)
        {
            sg.units.Add(new UnitDTO
            {
                unitId = u.def ? u.def.id : "worker",
                pos = u.transform.position
            });
        }

        var json = JsonUtility.ToJson(sg, true);
        File.WriteAllText(PathFor(slot), json);
        Debug.Log("Saved: " + PathFor(slot));
    }

    public void Load(string slot)
    {
        if (!registry)
        {
            Debug.LogError("SaveManager: registry not assigned!");
            return;
        }

        var p = PathFor(slot);
        if (!File.Exists(p))
        {
            Debug.LogWarning("No save file: " + p);
            return;
        }

        var json = File.ReadAllText(p);
        var sg = JsonUtility.FromJson<SaveGame>(json);

        // Clear dynamic objects
        foreach (var b in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            Destroy(b.gameObject);
        foreach (var s in Object.FindObjectsByType<Settlement>(FindObjectsSortMode.None))
            Destroy(s.gameObject);
        foreach (var u in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
            Destroy(u.gameObject);

        // Rebuild tiles
        var allTiles = Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
        foreach (var td in sg.tiles)
        {
            var t = FindNearest(allTiles, td.pos);
            if (!t) continue;

            t.biome = td.biome;
            t.subBiome = td.subBiome;
            t.temperature = td.temperature;
            t.moisture = td.moisture;
            t.ownerSettlementId = td.ownerSettlementId;
            t.worked = td.worked;

            t.resources.Clear();
            foreach (var pair in td.resources)
            {
                var sp = pair.Split(':');
                if (sp.Length != 2) continue;
                var res = registry.FindResource(sp[0]);
                if (!int.TryParse(sp[1], out int amt)) amt = 0;
                if (res) t.resources.Add(new ResourceStack { resource = res, amountPerDay = amt });
            }

            if (!string.IsNullOrEmpty(td.buildingId))
            {
                var def = registry.FindBuilding(td.buildingId);
                if (def != null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = t.transform.position + new Vector3(0, 0.15f, 0);
                    go.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
                    var bi = go.AddComponent<BuildingInstance>();
                    bi.ApplyTo(t, def);
                }
            }
        }

        // Rebuild settlements
        foreach (var sd in sg.settlements)
        {
            var go = new GameObject("Settlement");
            go.transform.position = sd.pos;
            var s = go.AddComponent<Settlement>();
            s.settlementId = sd.id;
            s.settlementName = sd.name;
            s.population = sd.population;
            s.InitOwnership();
            foreach (var kv in sd.stock)
            {
                var sp = kv.Split(':');
                if (sp.Length != 2) continue;
                var res = registry.FindResource(sp[0]);
                if (!int.TryParse(sp[1], out int amt)) amt = 0;
                if (res) s.Add(res, amt);
            }
        }

        // Rebuild units
        foreach (var ud in sg.units)
        {
            var udef = registry.FindUnit(ud.unitId);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale *= 0.6f;
            var u = go.AddComponent<Unit>();
            u.def = udef;
            var ti = FindNearest(allTiles, ud.pos);
            if (ti) u.SetTile(ti);
            var wc = go.AddComponent<WorkerController>();
            wc.unit = u;
        }

        dayCounter = sg.dayCount;
        Debug.Log("Loaded: " + p);
    }

    TileInfo FindNearest(TileInfo[] tiles, Vector3 pos)
    {
        TileInfo best = null;
        float dist = float.PositiveInfinity;
        foreach (var t in tiles)
        {
            float d = Vector3.SqrMagnitude(t.transform.position - pos);
            if (d < dist)
            {
                dist = d;
                best = t;
            }
        }
        return best;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) Save("slot1");
        if (Input.GetKeyDown(KeyCode.F9)) Load("slot1");
    }
}
