using MMDress.Customer;

namespace MMDress.UI
{
    /// Dipublish saat customer meninggalkan fitting (close panel / selesai),
    /// membawa info berapa item yang benar-benar ter-equip.
    public readonly struct CustomerCheckout
    {
        public readonly CustomerController customer;
        public readonly int itemsEquipped; // 0..2 (Top, Bottom)

        public CustomerCheckout(CustomerController customer, int itemsEquipped)
        {
            this.customer = customer;
            this.itemsEquipped = itemsEquipped;
        }
    }

    /// Dipublish tiap kali skor berubah (untuk HUD).
    public readonly struct ScoreChanged
    {
        public readonly int served;       // checkout dengan item > 0
        public readonly int empty;        // checkout item == 0
        public readonly int totalScore;   // metrik sederhana

        public ScoreChanged(int served, int empty, int totalScore)
        {
            this.served = served;
            this.empty = empty;
            this.totalScore = totalScore;
        }
    }
}
