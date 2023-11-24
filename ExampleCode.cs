namespace ProterOver
{
    public class Mus
    {
        public virtual void A()
        {

        }
    }

    public class ExampleCode : Mus
    {
        public override void A()
        {
            A();
        }
    }
}
