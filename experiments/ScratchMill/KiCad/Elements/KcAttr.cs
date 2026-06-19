namespace ScratchMill.KiCad.Elements
{
    public class KcAttr
    {
        public bool HasTeardrop { get; private set; }
        public string TeardropKind { get; private set; }
        public static KcAttr Parse(List<object> items)
        {
            var result = new KcAttr();
            string name = (string)items[0];
            switch (name)
            {
                case "teardrop":
                    result.HasTeardrop = true;
                    if (items[1] is List<object> typeValue && typeValue[0] is string typeName)
                    {
                        if (typeName == "type")
                        {
                            result.TeardropKind = typeValue[1].ToString();
                        }
                    }
                    break;
            }
            return result;
        }
    }
}
