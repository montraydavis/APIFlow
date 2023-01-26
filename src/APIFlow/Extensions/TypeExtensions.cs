namespace APIFlow.Extensions
{
    public static class TypeExtensions
    {
        private static Random rng = new Random();
        
        /// <summary>
        /// Shuffle a list.
        /// </summary>
        /// <typeparam name="T">Type of T.</typeparam>
        /// <param name="readOnlyList">List to shuffle.</param>
        /// <returns>Shuffled List.</returns>
        public static IReadOnlyList<T> Shuffle<T>(this IReadOnlyList<T> readOnlyList)
        {
            var list = readOnlyList.ToList();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list.AsReadOnly();
        }
    }
}
