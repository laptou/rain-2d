using System;
using Ibinimator.Model;
using Ibinimator.Service;
using Ibinimator.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ibinimator.Test
{
    [TestClass]
    public class WatcherTest
    {
        [TestMethod]
        public void Watcher()
        {
            var dummy = new Dummy
            {
                Int = 2,
                String = "asdf",
                Object = Guid.NewGuid()
            };
            dummy.IntList.Add(300);

            var watcher = new Watcher<int, Dummy>(dummy, d => d.GetHashCode());

            dummy.Int = 400324;
            dummy.String = "Whee";
            dummy.Object = new ObsoleteAttribute();
            dummy.IntList.Add(310);
            dummy.IntList.Add(320);
        }
    }

    public class Dummy : Model.Model
    {
        public Dummy()
        {
            IntList = new ObservableList<int>();
        }

        [Undoable]
        public int Int
        {
            get => Get<int>();
            set => Set(value);
        }

        [Undoable]
        public string String
        {
            get => Get<string>();
            set => Set(value);
        }

        [Undoable]
        public object Object
        {
            get => Get<object>();
            set => Set(value);
        }

        [Undoable]
        public Model.Model Model
        {
            get => Get<Model.Model>();
            set => Set(value);
        }

        [Undoable]
        public ObservableList<int> IntList
        {
            get => Get<ObservableList<int>>();
            set => Set(value);
        }
    }
}
