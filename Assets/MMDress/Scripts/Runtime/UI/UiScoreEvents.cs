using MMDress.Customer;

namespace MMDress.UI
{
    /// Dipublish saat customer meninggalkan fitting (checkout).
    /// itemsEquipped: 0..2 (Top, Bottom)
    public struct CustomerCheckout
    {
        public CustomerController customer;
        public int itemsEquipped;

        public CustomerCheckout(CustomerController customer, int itemsEquipped)
        {
            this.customer = customer;
            this.itemsEquipped = itemsEquipped;
        }
    }

    /// Dipublish tiap kali skor berubah (untuk HUD).
    public struct ScoreChanged
    {
        public int served;       // checkout dengan item > 0
        public int empty;        // checkout item == 0
        public int totalScore;   // metrik sederhana

        public ScoreChanged(int served, int empty, int totalScore)
        {
            this.served = served;
            this.empty = empty;
            this.totalScore = totalScore;
        }
    }

    /// (Opsional) Perubahan saldo—dipakai EconomyService.
    public struct MoneyChanged
    {
        public int amount;   // delta (+/-)
        public int balance;  // saldo akhir

        public MoneyChanged(int amount, int balance)
        {
            this.amount = amount;
            this.balance = balance;
        }
    }
}
