using Game.CharacterController;
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
namespace Game.CharacterController
{
    public class Weapon : MonoBehaviour
    {
        // Private variables
        private bool readyToShoot;
        private bool allowReset = true;
        private bool canShoot = true;       //For semi-auto, to prevent holding down the button
        private bool isReloading;
        private PlayerInput _playerInput;
        private int burstBulletsLeft;
        private int ammoDiviser = 1;      // Used to calculate the ammo display fill amount for shotguns
        private bool stayADS = false;
        private bool lockADS = true;

        public enum ShootingMode
        {
            SemiAuto,
            Burst,
            Auto
        }
        [Header("Shooting Mode")]
        public ShootingMode currentShootingMode;
        public bool isShotgun = false;
        public bool isHitscan = false;
        public bool toggleADS = true;

        [Header("Gun Settings")]
        public float shootingDelay = 1f;    //Fire rate
        public float inBetweenBurstDelay = 0.05f;
        public float spreadIntensity = 0.5f;
        public float reloadTime = 2f;
        public int magazineSize = 30;

        [Header("Bullet Settings")]
        public int bulletsLeft;
        public int bulletsPerBurst = 3;
        public float bulletVelocity = 50f;
        public float bulletGravity = 0.1f;
        public float bulletPrefabLifeTime = 3f;

        [Header("Components")]
        public Camera _playerCamera;
        public GameObject _bulletPrefab;
        public Transform _bulletSpawn;
        public Image _reloadProgressBar;

        private void Awake()
        {
            _reloadProgressBar.enabled = false;
            readyToShoot = true;
            burstBulletsLeft = bulletsPerBurst;
            _playerInput = GetComponent<PlayerInput>();

            bulletsLeft = magazineSize;

            if (isShotgun)
            {
                ammoDiviser = bulletsPerBurst;
            }
            else
            {
                ammoDiviser = 1;
            }
        }
        void Update()
        {
            // CanShoot logic for semi-auto: Only allow shooting again when the shoot button is released and pressed again
            if (currentShootingMode == ShootingMode.SemiAuto && readyToShoot && !_playerInput.ShootPressed && !isReloading)
            {
                canShoot = true;
            }
            else if (readyToShoot && currentShootingMode != ShootingMode.SemiAuto && !isReloading)
            {
                canShoot = true;
            }
            // Reloading logic
            if (_playerInput.ReloadPressed && bulletsLeft < magazineSize && !isReloading)
            {
                Reload();
            }
            // Auto-reload when trying to shoot with an empty magazine
            if (readyToShoot && bulletsLeft <= 0 && !isReloading && canShoot)
            {
                Reload();
            }
            // ADS logic
            if (_playerInput.ADSPressed)
            {
                if (toggleADS && !lockADS)
                {
                    stayADS = !stayADS;
                    lockADS = true;
                }
                else if (!toggleADS)
                {
                    stayADS = true;
                }
            }
            else if (!toggleADS)
            {
                stayADS = false;
            }
            if (stayADS)
            {
                _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, 35f, Time.deltaTime * 16f);
            }
            else
            {
                _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, 70f, Time.deltaTime * 16f);
            }
            if (toggleADS && !_playerInput.ADSPressed)
            {
                lockADS = false;
            }
            // Shooting logic
            if (_playerInput.ShootPressed && readyToShoot && canShoot)
            {
                burstBulletsLeft = bulletsPerBurst;
                FireWeapon();
            }

            // Update ammo display
            if (PlayerUiManager.Instance.ammoDisplay != null)
            {
                PlayerUiManager.Instance.ammoDisplay.text = $"{bulletsLeft/ammoDiviser}/{magazineSize/ammoDiviser}";
            }
        }
        private void FireWeapon()
        {
            bulletsLeft--;
            readyToShoot = false;
            canShoot = false;
            Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
            if (isHitscan)
            {
                RaycastHit hit;
                if (Physics.Raycast(_bulletSpawn.position, shootingDirection, out hit, 100f))
                {
                    // hit something
                    print("hitscan hit something");
                }
            }
            else
            {
                GameObject bullet = Instantiate(_bulletPrefab, _bulletSpawn.position, Quaternion.identity);
                bullet.transform.forward = shootingDirection;
                //bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);     stare zostawie na wszelki
                bullet.GetComponent<Rigidbody>().linearVelocity = shootingDirection * bulletVelocity;
                bullet.GetComponent<Rigidbody>().AddForce(Vector3.down * bulletGravity, ForceMode.Acceleration);
                StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));
            }

            if (allowReset)
            {
                Invoke("ResetShot", shootingDelay);
                allowReset = false;
            }
            if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1)
            {
                burstBulletsLeft--;
                Invoke("FireWeapon", inBetweenBurstDelay);
            }
        }
        private void Reload()
        {
            isReloading = true;
            canShoot = false;
            _reloadProgressBar.enabled = true;
            ReloadProgressBarUpdate();
            Invoke("ReloadFinished", reloadTime);
        }
        private void ReloadProgressBarUpdate()
        {
            _reloadProgressBar.fillAmount -= 0.01f;

            if (_reloadProgressBar.fillAmount <= 0)
            {
                _reloadProgressBar.enabled = false;
                _reloadProgressBar.fillAmount = 1.0f;
            }
            else
            {
                Invoke("ReloadProgressBarUpdate", reloadTime/99.0f);
            }
        }
        private void ReloadFinished()
        {
            bulletsLeft = magazineSize;
            canShoot = true;
            isReloading = false;
        }
        private void ResetShot()
        {
            readyToShoot = true;
            allowReset = true;
        }
        public Vector3 CalculateDirectionAndSpread() 
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Vector3 targetPoint;
            if (Physics.Raycast(ray, out hit))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(100);
            }
            Vector3 direction = targetPoint - _bulletSpawn.position;
            float spread_x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
            float spread_y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
            return direction + new Vector3(spread_x, spread_y, 0f);
        }
        private IEnumerator DestroyBulletAfterTime(GameObject bullet, float bulletPrefabLifeTime)
        {
            yield return new WaitForSeconds(bulletPrefabLifeTime);
            Destroy(bullet);
        }
    }

}
