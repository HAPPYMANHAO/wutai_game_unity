using UnityEngine;

namespace WutaiTactics
{
    public class CombatActor : MonoBehaviour
    {
        [Header("Stats")]
        public CombatRole role;
        public int maxHealth = 100;
        public int currentHealth;
        public int attackPower = 10;

        [Header("Visual")]
        public SpriteRenderer spriteRenderer;

        public bool IsAlive => currentHealth > 0;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = SpriteUtil.GetWhiteSprite();
            }
        }

        public void TakeDamage(int damage)
        {
            if (damage < 0) damage = 0;
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
        }

        public void Heal(int amount)
        {
            if (amount < 0) amount = 0;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }
    }
}
