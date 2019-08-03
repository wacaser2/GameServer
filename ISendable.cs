using System.IO;

internal interface ISendable {
    void ToSend(BinaryWriter bw);
}