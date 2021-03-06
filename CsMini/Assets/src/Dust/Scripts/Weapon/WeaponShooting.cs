using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponShooting : MonoBehaviour
{

    private float lastShootTime = 0;

    public bool weaponLoaded = false;

    [SerializeField] private int primaryCurrentAmmo;
    [SerializeField] private int primaryCurrentAmmoStorage;

    [SerializeField] private int secondaryCurrentAmmo;
    [SerializeField] private int secondaryCurrentAmmoStorage;

    [SerializeField] private bool primaryMagazineIsEmpty = false;
    [SerializeField] private bool secondaryMagazineIsEmpty = false;
    [SerializeField] private GameObject bloodPS = null;
    public bool canReload = true;

    private Camera cam;
    private Inventory inventory;
    private EquipmentManager manager;
    private Animator playerAnimator;
    private PlayerHUD playerHUD;
    private PlayerState playerState;
    private AttackButton attackButton;
    private ReloadButton reloadButton;
    private AudioSource audioSourceFire;
    private AudioClip audioClipFire;

    private void Start()
    {
        getReferences();
    }
    private void Update()
    {
        bool isPc = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        if ((isPc && Input.GetKeyDown(KeyCode.Mouse0)) || attackButton.pressed)
        {
            shoot();
        }
        if (Input.GetKeyDown(KeyCode.R) || reloadButton.pressed)
        {
            reload(manager.currentlyEquippedWeapon);
        }
    }


    private void RaycastShoot(Weapon currentWeapon)
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        float currentWeaponRange = currentWeapon.range;

        if (Physics.Raycast(ray, out hit, currentWeaponRange))
        {
            if (hit.transform.tag == "Enemy")
            {
                BotState enemyState = hit.transform.GetComponent<BotState>();
                enemyState.takeDamage(currentWeapon.damage);

                spawnBloodParticles(hit.point, hit.normal);

                if (enemyState.isDie())
                {
                    playerHUD.updateKill(playerState.getPlayerName(), enemyState.getPlayerName());
                }
            }
        }

        if (currentWeapon.weaponStyle != WeaponStyle.Melee)
            Instantiate(currentWeapon.muzzleFlashParticles, manager.currentWeaponBarrel);
    }

    private void shoot()
    {
        Weapon currentWeapon = inventory.getItem(manager.currentlyEquippedWeapon);

        if (currentWeapon == null)
            return;

        int slot = (int)currentWeapon.weaponStyle;

        bool canShoot = weaponLoaded && (!((slot == 0 && primaryMagazineIsEmpty) || (slot == 1 && secondaryMagazineIsEmpty)));

        if (canShoot && canReload)
        {


            if (Time.time > lastShootTime + currentWeapon.fireRate)
            {
                lastShootTime = Time.time;
                RaycastShoot(currentWeapon);
                useAmmo(slot, 1, 0);

                ///audio not set or weapon without sound
                if (audioClipFire == null)
                    return;

                if (audioSourceFire.isPlaying)
                    audioSourceFire.Stop();

                audioSourceFire.PlayOneShot(audioClipFire, 0.6f);
            }
        }

    }

    private void useAmmo(int slot, int currentAmmoUsed, int currentStoreAmmoUsed)
    {
        if (slot == 0)
        {
            primaryCurrentAmmo -= currentAmmoUsed;
            primaryCurrentAmmoStorage -= currentStoreAmmoUsed;
            if (primaryCurrentAmmo <= 1)
            {
                primaryMagazineIsEmpty = true;
                reload(slot);
            }
        }
        else if (slot == 1)
        {
            secondaryCurrentAmmo -= currentAmmoUsed;
            secondaryCurrentAmmoStorage -= currentStoreAmmoUsed;
            if (secondaryCurrentAmmo <= 0)
            {
                secondaryMagazineIsEmpty = true;
                reload(slot);
            }
        }
        updateAmmo(slot);
    }


    private void reload(int slot)
    {
        Weapon weapon = inventory.getItem(slot);

        if (weapon == null)
            return;

        int magazineSize = weapon.magazineSize;
        if (slot == 0)
        {
            if (!((primaryCurrentAmmo != magazineSize) && (primaryCurrentAmmo >= 0 && primaryCurrentAmmoStorage > 0)))
                return;

            int subAmmo = Mathf.Min(magazineSize, primaryCurrentAmmoStorage);


            primaryCurrentAmmoStorage = Mathf.Max(primaryCurrentAmmoStorage - (magazineSize - primaryCurrentAmmo), 0);

            primaryCurrentAmmo = subAmmo;

            primaryMagazineIsEmpty = false;
        }
        else if (slot == 1)
        {
            if (!((secondaryCurrentAmmo != magazineSize) && (secondaryCurrentAmmo >= 0 && secondaryCurrentAmmoStorage > 0)))
                return;

            int subAmmo = Mathf.Min(magazineSize, secondaryCurrentAmmoStorage + secondaryCurrentAmmo);

            secondaryCurrentAmmoStorage = Mathf.Max(secondaryCurrentAmmoStorage - (magazineSize - secondaryCurrentAmmo), 0);

            secondaryCurrentAmmo = subAmmo;

            secondaryMagazineIsEmpty = false;
        }
        playerAnimator.SetTrigger("reload");
        manager.currentWeaponAnimator.SetTrigger("reload");
        updateAmmo(slot);
    }


    public void initAmmo(int slot, Weapon weapon)
    {
        if (!canReload)
            return;

        if (slot == 0)
        {
            primaryCurrentAmmo = weapon.magazineSize;
            primaryCurrentAmmoStorage = weapon.storedAmmo;
        }
        else if (slot == 1)
        {
            secondaryCurrentAmmo = weapon.magazineSize;
            secondaryCurrentAmmoStorage = weapon.storedAmmo;
        }
    }


    private void updateAmmo(int slot)
    {
        int tmp1 = 0, tmp2 = 0;

        if (slot == 0)
        {
            tmp1 = primaryCurrentAmmo;
            tmp2 = primaryCurrentAmmoStorage;
        }
        else if (slot == 1)
        {
            tmp1 = secondaryCurrentAmmo;
            tmp2 = secondaryCurrentAmmoStorage;
        }

        playerHUD.updateAmmoUI(tmp1, tmp2);
    }

    private void spawnBloodParticles(Vector3 position, Vector3 normal)
    {
        Instantiate(bloodPS, position, Quaternion.FromToRotation(Vector3.up, normal));
    }

    public void setAudioClipFire(AudioClip audio)
    {
        audioClipFire = audio;
    }

    private void getReferences()
    {
        cam = GetComponentInChildren<Camera>();
        inventory = GetComponent<Inventory>();
        manager = GetComponent<EquipmentManager>();
        playerAnimator = GetComponentInChildren<Animator>();
        playerHUD = GetComponent<PlayerHUD>();
        attackButton = FindObjectOfType<AttackButton>();
        reloadButton = FindObjectOfType<ReloadButton>();
        playerState = FindObjectOfType<PlayerState>();
        audioSourceFire = GetComponent<AudioSource>();
    }
}
