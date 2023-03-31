public partial class BLESender
{
    public struct Receiver { 
        public BLEReceiver receiver; 
        public float magnitude;

        public Receiver(BLEReceiver receiver, float magnitude)
        {
            this.receiver = receiver;
            this.magnitude = magnitude;
        }
    }

}

