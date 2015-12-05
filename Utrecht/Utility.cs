using System;

namespace Utrecht {
	interface IWriter {
		void Write (string msg);
		void Write (string format, params object[] list);
		void WriteLine ();
		void WriteLine (string msg);
		void WriteLine (string format, params object[] list);
	}

	public class ConsoleWriter : IWriter {
		public void Write (string msg) {
			Console.Write(msg);
		}

		public void Write (string format, params object[] list) {
			Write(string.Format(format, list));
		}

		public void WriteLine () {
			Write("\n");
		}

		public void WriteLine (string msg) {
			Console.WriteLine(msg);
		}

		public void WriteLine (string format, params object[] list) {
			WriteLine(string.Format(format, list));
		}
	}
}
