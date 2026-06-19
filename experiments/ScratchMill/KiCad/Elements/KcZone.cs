namespace ScratchMill.KiCad.Elements
{
    public class KcZone : IKcCopperElement
    {
        public KcPosition Mid { get; set; } = new KcPosition();
        public double Width { get; set; }
        public int NetId { get; private set; }
        public string NetName { get; private set; }
        public string Layer { get; private set; }
        public string Name { get; private set; }
        public List<KcAttr> Attrs { get; private set; } = new List<KcAttr>();
        public string HatchType { get; private set; }
        public double HatchStep { get; private set; }
        public int Priority { get; private set; }
        public bool ConnectPads { get; private set; }
        public double MinThickness { get; private set; }
        public bool FilledAreasThickness { get; private set; }
        public bool Fill { get; private set; }
        public List<KcPosition> Polygon { get; private set; }
        public List<KcPosition> FilledPolygon { get; private set; }
        public string UUID { get; private set; }

        public KcPosition At => Polygon[0];
        public bool IsOnLayer(KcLayer layer) => Layer == layer.Name;
        public bool IsOnNet(KcNet net) => NetId == net.Id;

        public bool IsTeardrop => Attrs.FirstOrDefault(attr => attr.HasTeardrop) != null;

        public override string ToString() => $"zone{NetId}[{At.X} {At.Y}]";

        public static KcZone Parse(List<object> items)
        {
            var result = new KcZone();
            foreach (var item in items)
            {
                if (item is List<object> list && list.Count > 1)
                {
                    string name = (string)list[0];
                    switch (name)
                    {
                        case "net":
                            result.NetId = Convert.ToInt32(list[1]);
                            break;
                        case "net_name":
                            result.NetName = (string)list[1];
                            break;
                        case "layer":
                            result.Layer = (string)list[1];
                            break;
                        case "name":
                            result.Name = (string)list[1];
                            break;
                        case "hatch":
                            result.HatchType = (string)list[1];
                            result.HatchStep = Convert.ToDouble(list[2]);
                            break;
                        case "priority":
                            result.Priority = Convert.ToInt32(list[1]);
                            break;
                        case "attr":
                            if (list[1] is List<object> attrList)
                            {
                                result.Attrs.Add(KcAttr.Parse(attrList));
                            }
                            break;
                        case "connect_pads":
                            if (list[1] is string)
                            {
                                result.ConnectPads = true;
                            }
                            //result.ConnectPads = Convert.ToBoolean(list[1]); // add clearance
                            break;
                        case "min_thickness":
                            result.MinThickness = Convert.ToDouble(list[1]);
                            break;
                        case "filled_areas_thickness":
                            result.FilledAreasThickness = Convert.ToBoolean(list[1]);
                            break;
                        case "fill":
                            if (list[1] is string)
                            {
                                result.Fill = true;
                            }
                            //result.Fill = Convert.ToBoolean(list[1]); // add specs
                            break;
                        case "polygon":
                            result.Polygon = KcPosition.ParsePoints((List<object>)list[1]);
                            break;
                        case "filled_polygon":
                            result.FilledPolygon = KcPosition.ParsePoints((List<object>)list[1]);
                            break;
                        case "uuid":
                        case "tstamp":
                            result.UUID = (string)list[1];
                            break;
                    }
                }
            }
            return result;
        }
    }

}
