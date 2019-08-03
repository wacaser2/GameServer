using System.IO;

public interface ILoadable<T> {
    T Load(BinaryReader br);
}