
namespace UnknownCreator.Modules
{
    public class StatsCalc : IReference
    {
        public string name { get; set; }
        public BuffBase buff { get; set; }

        public CalcType calcType { get; set; }

        public int order { get; set; }

        public double value { get; set; }

        public void ObjRelease()
        {
            name = string.Empty;
            buff = null;
            value = 0;
            order = 0;
        }


        /*  public StatsCalc(string name, BuffBase buff, CalcType calcType, double value)
          {
              this.name = name;
              this.buff = buff;
              this.calcType = calcType;
              this.value = value;
              this.order = (int)calcType;
          }   */
    }
}