using UnityEngine;

namespace ArenaPrototype
{
    public class EquipmentPickup : MonoBehaviour
    {
        private ArenaGame game;
        private EquipmentItem item;
        private Vector3 startPosition;
        private Transform iconTransform;

        public void Setup(ArenaGame owner, EquipmentItem equipment)
        {
            game = owner;
            item = equipment;
            startPosition = transform.position;
            BuildIcon();
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.12f);
            if (iconTransform != null && Camera.main != null)
            {
                iconTransform.rotation = Camera.main.transform.rotation;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || item == null)
            {
                return;
            }

            game.Equipment.Equip(item);
            Destroy(gameObject);
        }

        private void BuildIcon()
        {
            if (game == null || game.Art == null || item == null)
            {
                return;
            }

            Sprite icon = game.Art.GetItemIcon(item.iconId);
            if (icon == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("Loot Icon");
            iconObject.transform.SetParent(transform, false);
            iconObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            iconObject.transform.localScale = Vector3.one * 0.2f;
            SpriteRenderer renderer = iconObject.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.sortingOrder = 12;
            renderer.color = Color.white;
            iconTransform = iconObject.transform;
        }
    }
}
