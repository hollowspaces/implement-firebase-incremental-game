using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fungsi System.Serializable adalah agar object bisa di-serialize dan value dapat di-set dari inspector
[System.Serializable]
public struct ResourceConfig
{
    public string Name;
    public double UnlockCost;
    public double UpgradeCost;
    public double Output;
}

public class GameManager : MonoBehaviour
{
    public AudioSource BuyItemSfx;

    // Store objects
    public Button ribbonButton, ballButton, milkButton;
    public GameObject ribbonSoldText, ballSoldText, milkSoldText;
    public Text ribbonPriceText, ballPriceText, milkPriceText, ribbonPriceAfterSoldText, ballPriceAfterSoldText, milkPriceAfterSoldText;

    // Game panel objects
    public GameObject hairRibbon, ballOfYarn, milk;

    private bool isRibbonSold, isBallSold, isMilkSold;
    public int ribbonPrice = 2000000, ballPrice = 4000000, milkPrice = 2500000;

    private static GameManager _instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }

            return _instance;
        }
    }

    // Fungsi [Range (min, max)] ialah menjaga value agar tetap berada di antara min dan max-nya
    [Range(0f, 1f)]
    public float AutoCollectPercentage = 0.1f;
    public ResourceConfig[] ResourcesConfigs;
    public Sprite[] ResourcesSprites;

    public Transform ResourcesParent;
    public ResourceController ResourcePrefab;
    public TapText TapTextPrefab;

    public Transform CoinIcon;
    public Text GoldInfo;
    public Text AutoCollectInfo;

    private List<ResourceController> _activeResources = new List<ResourceController>();
    private List<TapText> _tapTextPool = new List<TapText>();
    private float _collectSecond;

    //public double _totalGold;

    private void Start()
    {
        AddAllResources();
        GoldInfo.text = $"Gold: { UserDataManager.Progress.Gold.ToString("0") }";

        // menonaktifkan semua item di awal
        hairRibbon.gameObject.SetActive(false);
        ballOfYarn.gameObject.SetActive(false);
        milk.gameObject.SetActive(false);

        // menonaktifkan button di store
        ribbonButton.interactable = false;
        ballButton.interactable = false;
        milkButton.interactable = false;

        // mengatur text item yang belum terjual
        ribbonSoldText.gameObject.SetActive(false);
        ballSoldText.gameObject.SetActive(false);
        milkSoldText.gameObject.SetActive(false);

        // mengatur harga item yang akan dijual
        ribbonPriceText.text = ribbonPrice.ToString() + " Meows";
        ballPriceText.text = ballPrice.ToString() + " Meows";
        milkPriceText.text = milkPrice.ToString() + " Meows";

        // menonaktifkan text
        ribbonPriceAfterSoldText.gameObject.SetActive(false);
        ballPriceAfterSoldText.gameObject.SetActive(false);
        milkPriceAfterSoldText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Fungsi untuk selalu mengeksekusi CollectPerSecond setiap detik
        _collectSecond += Time.unscaledDeltaTime;
        if (_collectSecond >= 1f)
        {
            CollectPerSecond();
            _collectSecond = 0f;
        }

        CheckResourceCost();

        CoinIcon.transform.localScale = Vector3.LerpUnclamped(CoinIcon.transform.localScale, Vector3.one * 0.25f, 0.15f);
        // CoinIcon.transform.Rotate(0f, 0f, Time.deltaTime * -100f);

        DoYouHaveEnoughPawsToBuySmth();
    }

    public void SellRibbon()
    {
        hairRibbon.gameObject.SetActive(true);
        UserDataManager.Progress.Gold -= ribbonPrice;
        isRibbonSold = true;
        ribbonSoldText.gameObject.SetActive(true);
        ribbonPriceText.gameObject.SetActive(false);

        ribbonPriceAfterSoldText.text = ribbonPrice.ToString() + " Meows";
        ribbonPriceAfterSoldText.gameObject.SetActive(true);

        BuyItemSfx.Play();
    }

    public void SellBall()
    {
        ballOfYarn.gameObject.SetActive(true);
        UserDataManager.Progress.Gold -= ballPrice;
        isBallSold = true;
        ballSoldText.gameObject.SetActive(true);
        ballPriceText.gameObject.SetActive(false);

        ballPriceAfterSoldText.text = ballPrice.ToString() + " Meows";
        ballPriceAfterSoldText.gameObject.SetActive(true);

        BuyItemSfx.Play();
    }

    public void SellMilk()
    {
        milk.gameObject.SetActive(true);
        UserDataManager.Progress.Gold -= milkPrice;
        isMilkSold = true;
        milkSoldText.gameObject.SetActive(true);
        milkPriceText.gameObject.SetActive(false);

        milkPriceAfterSoldText.text = milkPrice.ToString() + " Meows";
        milkPriceAfterSoldText.gameObject.SetActive(true);

        BuyItemSfx.Play();
    }

    // mengecek apakah pawgold cukup untuk membeli item yang dijual
    void DoYouHaveEnoughPawsToBuySmth()
    {
        if (UserDataManager.Progress.Gold < ribbonPrice)
            ribbonButton.interactable = false;

        if (UserDataManager.Progress.Gold < ballPrice)
            ballButton.interactable = false;

        if (UserDataManager.Progress.Gold < milkPrice)
            milkButton.interactable = false;


        if (!isRibbonSold && UserDataManager.Progress.Gold >= ribbonPrice)
        {
            ribbonButton.interactable = true;
        }

        if (!isBallSold && UserDataManager.Progress.Gold >= ballPrice)
        {
            ballButton.interactable = true;
        }

        if (!isMilkSold && UserDataManager.Progress.Gold >= milkPrice)
        {
            milkButton.interactable = true;
        }
    }

    private void AddAllResources()
    {
        bool showResources = true;
        int index = 0;
        foreach (ResourceConfig config in ResourcesConfigs)
        {
            GameObject obj = Instantiate(ResourcePrefab.gameObject, ResourcesParent, false);
            ResourceController resource = obj.GetComponent<ResourceController>();

            resource.SetConfig(index, config);
            obj.gameObject.SetActive(showResources);

            if (showResources && !resource.IsUnlocked)
            {
                showResources = false;
            }

            _activeResources.Add(resource);
            index++;
        }
    }

    public void ShowNextResource()
    {
        foreach (ResourceController resource in _activeResources)
        {
            if (!resource.gameObject.activeSelf)
            {
                resource.gameObject.SetActive(true);
                break;
            }
        }
    }

    private void CheckResourceCost()
    {
        foreach (ResourceController resource in _activeResources)
        {
            bool isBuyable = false;
            if (resource.IsUnlocked)
            {
                isBuyable = UserDataManager.Progress.Gold >= resource.GetUpgradeCost();
            }
            else
            {
                isBuyable = UserDataManager.Progress.Gold >= resource.GetUnlockCost();
            }

            resource.ResourceImage.sprite = ResourcesSprites[isBuyable ? 1 : 0];
        }
    }

    private void CollectPerSecond()
    {
        double output = 0;
        foreach (ResourceController resource in _activeResources)
        {
            if (resource.IsUnlocked)
            {
                output += resource.GetOutput();
            }
        }

        output *= AutoCollectPercentage;
        // Fungsi ToString("F1") ialah membulatkan angka menjadi desimal yang memiliki 1 angka di belakang koma
        AutoCollectInfo.text = $"Auto Collect: { output.ToString("F1") } / second";

        AddGold(output);
    }

    public void AddGold(double value)
    {
        UserDataManager.Progress.Gold += value;
        GoldInfo.text = $"Meow(s): { UserDataManager.Progress.Gold.ToString("0") }";

        UserDataManager.Save();
    }

    public void CollectByTap(Vector3 tapPosition, Transform parent)
    {
        double output = 0;
        foreach (ResourceController resource in _activeResources)
        {
            if (resource.IsUnlocked)
            {
                output += resource.GetOutput();
            }
        }

        TapText tapText = GetOrCreateTapText();
        tapText.transform.SetParent(parent, false);
        tapText.transform.position = tapPosition;

        tapText.Text.text = $"+{ output.ToString("0") }";
        tapText.gameObject.SetActive(true);
        CoinIcon.transform.localScale = Vector3.one * 0.30f;

        AddGold(output);
    }

    private TapText GetOrCreateTapText()
    {
        TapText tapText = _tapTextPool.Find(t => !t.gameObject.activeSelf);
        if (tapText == null)
        {
            tapText = Instantiate(TapTextPrefab).GetComponent<TapText>();
            _tapTextPool.Add(tapText);
        }

        return tapText;
    }
}

