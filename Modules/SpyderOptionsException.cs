public class SpyderOptionsException : Exception
    {
        public SpyderOptionsException()
            {
            }





        public SpyderOptionsException(string message)
            : base(message)
            {
            }





        public SpyderOptionsException(string message, Exception inner)
            : base(message, inner)
            {
            }
    }