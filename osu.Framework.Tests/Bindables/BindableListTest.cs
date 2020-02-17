﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableListTest
    {
        private BindableList<string> bindableStringList;

        [SetUp]
        public void SetUp()
        {
            bindableStringList = new BindableList<string>();
        }

        #region Constructor

        [Test]
        public void TestConstructorDoesNotAddItemsByDefault()
        {
            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestConstructorWithItemsAddsItemsInternally()
        {
            string[] array =
            {
                "ok", "nope", "random", null, ""
            };

            var bindableList = new BindableList<string>(array);

            Assert.Multiple(() =>
            {
                foreach (string item in array)
                    Assert.Contains(item, bindableList);

                Assert.AreEqual(array.Length, bindableList.Count);
            });
        }

        #endregion

        #region BindTarget

        /// <summary>
        /// Tests binding via the various <see cref="BindableList{T}.BindTarget"/> methods.
        /// </summary>
        [Test]
        public void TestBindViaBindTarget()
        {
            BindableList<int> parentBindable = new BindableList<int>();

            BindableList<int> bindable1 = new BindableList<int>();
            IBindableList<int> bindable2 = new BindableList<int>();

            bindable1.BindTarget = parentBindable;
            bindable2.BindTarget = parentBindable;

            parentBindable.Add(5);

            Assert.That(bindable1[0], Is.EqualTo(5));
            Assert.That(bindable2[0], Is.EqualTo(5));
        }

        #endregion

        #region list[index]

        [Test]
        public void TestGetRetrievesObjectAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            Assert.AreEqual("1", bindableStringList[1]);
        }

        [Test]
        public void TestSetMutatesObjectAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList[1] = "2";

            Assert.AreEqual("2", bindableStringList[1]);
        }

        [Test]
        public void TestGetWhileDisabledDoesNotThrowInvalidOperationException()
        {
            bindableStringList.Add("0");
            bindableStringList.Disabled = true;

            Assert.AreEqual("0", bindableStringList[0]);
        }

        [Test]
        public void TestSetWhileDisabledThrowsInvalidOperationException()
        {
            bindableStringList.Add("0");
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringList[0] = "1");
        }

        [Test]
        public void TestSetNotifiesSubscribers()
        {
            bindableStringList.Add("0");

#pragma warning disable 618 can be removed 20200817
            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;
            bindableStringList.ItemsAdded += v => addedItem = v;
            bindableStringList.ItemsRemoved += v => removedItem = v;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList[0] = "1";

            Assert.That(removedItem.Single(), Is.EqualTo("0"));
            Assert.That(addedItem.Single(), Is.EqualTo("1"));

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo("0".Yield()));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo("1".Yield()));
            Assert.That(triggeredArgs.OldStartingIndex, Is.Zero);
            Assert.That(triggeredArgs.NewStartingIndex, Is.Zero);
        }

        [Test]
        public void TestSetNotifiesBoundLists()
        {
            bindableStringList.Add("0");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            IEnumerable<string> addedItem = null;
            IEnumerable<string> removedItem = null;
            list.ItemsAdded += v => addedItem = v;
            list.ItemsRemoved += v => removedItem = v;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList[0] = "1";

            Assert.That(removedItem.Single(), Is.EqualTo("0"));
            Assert.That(addedItem.Single(), Is.EqualTo("1"));

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo("0".Yield()));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo("1".Yield()));
            Assert.That(triggeredArgs.OldStartingIndex, Is.Zero);
            Assert.That(triggeredArgs.NewStartingIndex, Is.Zero);
        }

        #endregion

        #region .Add(item)

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringAddsStringToEnumerator(string str)
        {
            bindableStringList.Add(str);

            Assert.Contains(str, bindableStringList);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriber(string str)
        {
#pragma warning disable 618 can be removed 20200817
            string addedString = null;
            bindableStringList.ItemsAdded += s => addedString = s.SingleOrDefault();
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Add(str);

            Assert.AreEqual(str, addedString);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(str.Yield()));
            Assert.That(triggeredArgs.NewStartingIndex, Is.Zero);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesSubscriberOnce(string str)
        {
#pragma warning disable 618 can be removed 20200817
            int notificationCount = 0;
            bindableStringList.ItemsAdded += s => notificationCount++;
#pragma warning restore 618
            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringList.Add(str);

            Assert.AreEqual(1, notificationCount);

            Assert.That(triggeredArgs, Has.Count.EqualTo(1));
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesMultipleSubscribers(string str)
        {
#pragma warning disable 618 can be removed 20200817
            bool subscriberANotified = false;
            bool subscriberBNotified = false;
            bool subscriberCNotified = false;
            bindableStringList.ItemsAdded += s => subscriberANotified = true;
            bindableStringList.ItemsAdded += s => subscriberBNotified = true;
            bindableStringList.ItemsAdded += s => subscriberCNotified = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringList.Add(str);

            Assert.IsTrue(subscriberANotified);
            Assert.IsTrue(subscriberBNotified);
            Assert.IsTrue(subscriberCNotified);

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesMultipleSubscribersOnlyAfterTheAdd(string str)
        {
#pragma warning disable 618 can be removed 20200817
            bool subscriberANotified = false;
            bool subscriberBNotified = false;
            bool subscriberCNotified = false;
            bindableStringList.ItemsAdded += s => subscriberANotified = true;
            bindableStringList.ItemsAdded += s => subscriberBNotified = true;
            bindableStringList.ItemsAdded += s => subscriberCNotified = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsC = args;

            Assert.IsFalse(subscriberANotified);
            Assert.IsFalse(subscriberBNotified);
            Assert.IsFalse(subscriberCNotified);

            Assert.That(triggeredArgsA, Is.Null);
            Assert.That(triggeredArgsB, Is.Null);
            Assert.That(triggeredArgsC, Is.Null);

            bindableStringList.Add(str);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundList(string str)
        {
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Add(str);

            Assert.Contains(str, list);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithStringNotifiesBoundLists(string str)
        {
            var listA = new BindableList<string>();
            var listB = new BindableList<string>();
            var listC = new BindableList<string>();
            listA.BindTo(bindableStringList);
            listB.BindTo(bindableStringList);
            listC.BindTo(bindableStringList);

            bindableStringList.Add(str);

            Assert.Contains(str, listA);
            Assert.Contains(str, listB);
            Assert.Contains(str, listC);
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithDisabledListThrowsInvalidOperationException(string str)
        {
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringList.Add(str); });
        }

        [TestCase("a random string")]
        [TestCase("", Description = "Empty string")]
        [TestCase(null)]
        public void TestAddWithListContainingItemsDoesNotOverrideItems(string str)
        {
            const string item = "existing string";
            bindableStringList.Add(item);

            bindableStringList.Add(str);

            Assert.Contains(item, bindableStringList);
        }

        #endregion

        #region .AddRange(items)

        [Test]
        public void TestAddRangeAddsItemsToEnumerator()
        {
            string[] items =
            {
                "A", "B", "C", "D"
            };

            bindableStringList.AddRange(items);

            Assert.Multiple(() =>
            {
                foreach (string item in items)
                    Assert.Contains(item, bindableStringList);
            });
        }

        [Test]
        public void TestAddRangeNotifiesBoundLists()
        {
            string[] items = { "test1", "test2", "test3" };
            var list = new BindableList<string>();
            bindableStringList.BindTo(list);

#pragma warning disable 618 can be removed 20200817
            IEnumerable<string> addedItems = null;
            list.ItemsAdded += e => addedItems = e;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.AddRange(items);

            Assert.Multiple(() =>
            {
                Assert.NotNull(addedItems);
                CollectionAssert.AreEquivalent(items, addedItems);
                CollectionAssert.AreEquivalent(items, list);
            });

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo(items));
        }

        [Test]
        public void TestAddRangeEnumeratesOnlyOnce()
        {
            BindableList<int> list1 = new BindableList<int>();
            BindableList<int> list2 = new BindableList<int>();
            list2.BindTo(list1);

            int counter = 0;

            IEnumerable<int> valueEnumerable()
            {
                yield return counter++;
            }

            list1.AddRange(valueEnumerable());

            Assert.That(list1, Is.EquivalentTo(0.Yield()));
            Assert.That(list2, Is.EquivalentTo(0.Yield()));
            Assert.That(counter, Is.EqualTo(1));
        }

        #endregion

        #region .Insert

        [Test]
        public void TestInsertInsertsItemAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            bindableStringList.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", bindableStringList[0]);
                Assert.AreEqual("1", bindableStringList[1]);
                Assert.AreEqual("2", bindableStringList[2]);
            });
        }

        [Test]
        public void TestInsertNotifiesSubscribers()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

#pragma warning disable 618 can be removed 20200817
            bool wasAdded = false;
            bindableStringList.ItemsAdded += _ => wasAdded = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Insert(1, "1");

            Assert.IsTrue(wasAdded);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Has.One.Items.EqualTo("1"));
            Assert.That(triggeredArgs.NewStartingIndex, Is.EqualTo(1));
        }

        [Test]
        public void TestInsertNotifiesBoundLists()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool wasAdded = false;
            list.ItemsAdded += _ => wasAdded = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Insert(1, "1");

            Assert.IsTrue(wasAdded);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Has.One.Items.EqualTo("1"));
            Assert.That(triggeredArgs.NewStartingIndex, Is.EqualTo(1));
        }

        [Test]
        public void TestInsertInsertsItemAtIndexInBoundList()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("2");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Insert(1, "1");

            Assert.Multiple(() =>
            {
                Assert.AreEqual("0", list[0]);
                Assert.AreEqual("1", list[1]);
                Assert.AreEqual("2", list[2]);
            });
        }

        #endregion

        #region .Remove(item)

        [Test]
        public void TestRemoveWithDisabledListThrowsInvalidOperationException()
        {
            const string item = "hi";
            bindableStringList.Add(item);
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Remove(item));
        }

        [Test]
        public void TestRemoveWithAnItemThatIsNotInTheListReturnsFalse()
        {
            bool gotRemoved = bindableStringList.Remove("hm");

            Assert.IsFalse(gotRemoved);
        }

        [Test]
        public void TestRemoveWhenListIsDisabledThrowsInvalidOperationException()
        {
            const string item = "item";
            bindableStringList.Add(item);
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => { bindableStringList.Remove(item); });
        }

        [Test]
        public void TestRemoveWithAnItemThatIsInTheListReturnsTrue()
        {
            const string item = "item";
            bindableStringList.Add(item);

            bool gotRemoved = bindableStringList.Remove(item);

            Assert.IsTrue(gotRemoved);
        }

        [Test]
        public void TestRemoveNotifiesSubscriber()
        {
            const string item = "item";
            bindableStringList.Add(item);

#pragma warning disable 618 can be removed 20200817
            bool updated = false;
            bindableStringList.ItemsRemoved += s => updated = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Remove(item);

            Assert.True(updated);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo(item));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestRemoveNotifiesSubscribers()
        {
            const string item = "item";
            bindableStringList.Add(item);

#pragma warning disable 618 can be removed 20200817
            bool updatedA = false;
            bool updatedB = false;
            bool updatedC = false;
            bindableStringList.ItemsRemoved += s => updatedA = true;
            bindableStringList.ItemsRemoved += s => updatedB = true;
            bindableStringList.ItemsRemoved += s => updatedC = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.True(updatedA);
                Assert.True(updatedB);
                Assert.True(updatedC);
            });

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [Test]
        public void TestRemoveNotifiesBoundList()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Remove(item);

            Assert.IsEmpty(list);
        }

        [Test]
        public void TestRemoveNotifiesBoundLists()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var listA = new BindableList<string>();
            listA.BindTo(bindableStringList);
            var listB = new BindableList<string>();
            listB.BindTo(bindableStringList);
            var listC = new BindableList<string>();
            listC.BindTo(bindableStringList);

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.False(listA.Contains(item));
                Assert.False(listB.Contains(item));
                Assert.False(listC.Contains(item));
            });
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscription()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool wasRemoved = false;
            list.ItemsRemoved += s => wasRemoved = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Remove(item);

            Assert.True(wasRemoved);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo(item));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestRemoveNotifiesBoundListSubscriptions()
        {
            const string item = "item";
            bindableStringList.Add(item);
            var listA = new BindableList<string>();
            listA.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool wasRemovedA1 = false;
            bool wasRemovedA2 = false;
            listA.ItemsRemoved += s => wasRemovedA1 = true;
            listA.ItemsRemoved += s => wasRemovedA2 = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsA1 = null;
            NotifyCollectionChangedEventArgs triggeredArgsA2 = null;
            listA.CollectionChanged += (_, args) => triggeredArgsA1 = args;
            listA.CollectionChanged += (_, args) => triggeredArgsA2 = args;

            var listB = new BindableList<string>();
            listB.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool wasRemovedB1 = false;
            bool wasRemovedB2 = false;
            listB.ItemsRemoved += s => wasRemovedB1 = true;
            listB.ItemsRemoved += s => wasRemovedB2 = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsB1 = null;
            NotifyCollectionChangedEventArgs triggeredArgsB2 = null;
            listB.CollectionChanged += (_, args) => triggeredArgsB1 = args;
            listB.CollectionChanged += (_, args) => triggeredArgsB2 = args;

            bindableStringList.Remove(item);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(wasRemovedA1);
                Assert.IsTrue(wasRemovedA2);
                Assert.IsTrue(wasRemovedB1);
                Assert.IsTrue(wasRemovedB2);
            });

            Assert.That(triggeredArgsA1, Is.Not.Null);
            Assert.That(triggeredArgsA2, Is.Not.Null);
            Assert.That(triggeredArgsB1, Is.Not.Null);
            Assert.That(triggeredArgsB2, Is.Not.Null);
        }

        [Test]
        public void TestRemoveDoesNotNotifySubscribersBeforeItemIsRemoved()
        {
            const string item = "item";
            bindableStringList.Add(item);

#pragma warning disable 618
            bool wasRemoved = false;
            bindableStringList.ItemsRemoved += _ => wasRemoved = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            Assert.That(wasRemoved, Is.False);

            Assert.That(triggeredArgs, Is.Null);
        }

        #endregion

        #region .RemoveRange(index, count)

        [TestCase(1, 0, 1)]
        [TestCase(0, 0, 0)]
        [TestCase(1000, 999, 1)]
        [TestCase(3, 1, 1)]
        [TestCase(10, 0, 9)]
        [TestCase(10, 0, 0)]
        public void TestRemoveRangeRemovesRange(int totalCount, int startIndex, int removeCount)
        {
            for (int i = 0; i < totalCount; i++)
                bindableStringList.Add("test" + i);

            bindableStringList.RemoveRange(startIndex, removeCount);

            Assert.AreEqual(totalCount - removeCount, bindableStringList.Count);

            var remainingItems = new List<string>();

            for (int i = 0; i < startIndex; i++)
                remainingItems.Add("test" + i);
            for (int i = startIndex + removeCount; i < totalCount; i++)
                remainingItems.Add("test" + i);

            CollectionAssert.AreEqual(remainingItems, bindableStringList);
        }

        [Test]
        public void TestRemoveRangeNotifiesSubscribers()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");

#pragma warning disable 618 can be removed 20200817
            List<string> itemsRemoved = null;
            bindableStringList.ItemsRemoved += i => itemsRemoved = i.ToList();
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveRange(1, 1);

            Assert.AreEqual(1, bindableStringList.Count);
            Assert.AreEqual("0", bindableStringList.FirstOrDefault());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, itemsRemoved.Count);
                Assert.AreEqual("1", itemsRemoved.FirstOrDefault());
            });

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo("1"));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(1));
        }

        [Test]
        public void TestRemoveRangeNotifiesBoundLists()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            List<string> itemsRemoved = null;
            list.ItemsRemoved += i => itemsRemoved = i.ToList();
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveRange(0, 1);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, itemsRemoved.Count);
                Assert.AreEqual("0", itemsRemoved.FirstOrDefault());
            });

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo("0"));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestRemoveRangeDoesNotNotifyBoundListsWhenCountIsZero()
        {
            bindableStringList.Add("0");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool notified = false;
            list.ItemsRemoved += i => notified = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveRange(0, 0);

            Assert.IsFalse(notified);

            Assert.That(triggeredArgs, Is.Null);
        }

        #endregion

        #region .RemoveAt(index)

        [Test]
        public void TestRemoveAtRemovesItemAtIndex()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            bindableStringList.RemoveAt(1);

            Assert.AreEqual("0", bindableStringList[0]);
            Assert.AreEqual("2", bindableStringList[1]);
        }

        [Test]
        public void TestRemoveAtWithDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Add("abc");
            bindableStringList.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindableStringList.RemoveAt(0));
        }

        [Test]
        public void TestRemoveAtNotifiesSubscribers()
        {
            bindableStringList.Add("abc");

#pragma warning disable 618 can be removed 20200817
            bool wasRemoved = false;
            bindableStringList.ItemsRemoved += _ => wasRemoved = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveAt(0);

            Assert.IsTrue(wasRemoved);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo("abc"));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestRemoveAtNotifiesBoundLists()
        {
            bindableStringList.Add("abc");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            bool wasRemoved = false;
            list.ItemsRemoved += s => wasRemoved = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveAt(0);

            Assert.IsTrue(wasRemoved);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Has.One.Items.EqualTo("abc"));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        #endregion

        #region .RemoveAll(match)

        [Test]
        public void TestRemoveAllRemovesMatchingElements()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");
            bindableStringList.Add("0");
            bindableStringList.Add("1");
            bindableStringList.Add("2");

            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, bindableStringList.Count);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("1", bindableStringList[0]);
                Assert.AreEqual("2", bindableStringList[1]);
            });
        }

        [Test]
        public void TestRemoveAllNotifiesSubscribers()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");

#pragma warning disable 618 can be removed 20200817
            List<string> itemsRemoved = null;
            bindableStringList.ItemsRemoved += i => itemsRemoved = i.ToList();
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new[] { "0", "0" }));
        }

        [Test]
        public void TestRemoveAllNotifiesBoundLists()
        {
            bindableStringList.Add("0");
            bindableStringList.Add("0");

            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

#pragma warning disable 618 can be removed 20200817
            List<string> itemsRemoved = null;
            list.ItemsRemoved += i => itemsRemoved = i.ToList();
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            list.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.RemoveAll(m => m == "0");

            Assert.AreEqual(2, itemsRemoved.Count);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new[] { "0", "0" }));
        }

        #endregion

        #region .Clear()

        [Test]
        public void TestClear()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

            bindableStringList.Clear();

            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestClearWithDisabledListThrowsInvalidOperationException()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Clear());
        }

        [Test]
        public void TestClearWithEmptyDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Disabled = true;

            Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Clear());
        }

        [Test]
        public void TestClearUpdatesCountProperty()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

            bindableStringList.Clear();

            Assert.AreEqual(0, bindableStringList.Count);
        }

        [Test]
        public void TestClearNotifiesSubscriber()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

#pragma warning disable 618 can be removed 20200817
            bool wasNotified = false;
            bindableStringList.ItemsRemoved += items => wasNotified = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Clear();

            Assert.IsTrue(wasNotified);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.OldItems, Is.EquivalentTo(new[] { "testA", "testA", "testA", "testA", "testA" }));
            Assert.That(triggeredArgs.OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestClearDoesNotNotifySubscriberBeforeClear()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

#pragma warning disable 618 can be removed 20200817
            bool wasNotified = false;
            bindableStringList.ItemsRemoved += items => wasNotified = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs = args;

            Assert.IsFalse(wasNotified);

            Assert.That(triggeredArgs, Is.Null);

            bindableStringList.Clear();
        }

        [Test]
        public void TestClearNotifiesSubscribers()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");

#pragma warning disable 618 can be removed 20200817
            bool wasNotifiedA = false;
            bool wasNotifiedB = false;
            bool wasNotifiedC = false;
            bindableStringList.ItemsRemoved += items => wasNotifiedA = true;
            bindableStringList.ItemsRemoved += items => wasNotifiedB = true;
            bindableStringList.ItemsRemoved += items => wasNotifiedC = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgsA = null;
            NotifyCollectionChangedEventArgs triggeredArgsB = null;
            NotifyCollectionChangedEventArgs triggeredArgsC = null;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsA = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsB = args;
            bindableStringList.CollectionChanged += (_, args) => triggeredArgsC = args;

            bindableStringList.Clear();

            Assert.Multiple(() =>
            {
                Assert.IsTrue(wasNotifiedA);
                Assert.IsTrue(wasNotifiedB);
                Assert.IsTrue(wasNotifiedC);
            });

            Assert.That(triggeredArgsA, Is.Not.Null);
            Assert.That(triggeredArgsB, Is.Not.Null);
            Assert.That(triggeredArgsC, Is.Not.Null);
        }

        [Test]
        public void TestClearNotifiesBoundBindable()
        {
            var bindableList = new BindableList<string>();
            bindableList.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableList.Add("testA");

            bindableStringList.Clear();

            Assert.IsEmpty(bindableList);
        }

        [Test]
        public void TestClearNotifiesBoundBindables()
        {
            var bindableListA = new BindableList<string>();
            bindableListA.BindTo(bindableStringList);
            var bindableListB = new BindableList<string>();
            bindableListB.BindTo(bindableStringList);
            var bindableListC = new BindableList<string>();
            bindableListC.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListC.Add("testA");

            bindableStringList.Clear();

            Assert.Multiple(() =>
            {
                Assert.IsEmpty(bindableListA);
                Assert.IsEmpty(bindableListB);
                Assert.IsEmpty(bindableListC);
            });
        }

        [Test]
        public void TestClearDoesNotNotifyBoundBindablesBeforeClear()
        {
            var bindableListA = new BindableList<string>();
            bindableListA.BindTo(bindableStringList);
            var bindableListB = new BindableList<string>();
            bindableListB.BindTo(bindableStringList);
            var bindableListC = new BindableList<string>();
            bindableListC.BindTo(bindableStringList);
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListA.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListB.Add("testA");
            for (int i = 0; i < 5; i++)
                bindableListC.Add("testA");

            Assert.Multiple(() =>
            {
                Assert.IsNotEmpty(bindableListA);
                Assert.IsNotEmpty(bindableListB);
                Assert.IsNotEmpty(bindableListC);
            });

            bindableStringList.Clear();
        }

        #endregion

        #region .CopyTo(array, index)

        [Test]
        public void TestCopyTo()
        {
            for (int i = 0; i < 5; i++)
                bindableStringList.Add("test" + i);
            string[] array = new string[5];

            bindableStringList.CopyTo(array, 0);

            CollectionAssert.AreEquivalent(bindableStringList, array);
        }

        #endregion

        #region .Disabled

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscriber()
        {
            bool? isDisabled = null;
            bindableStringList.DisabledChanged += b => isDisabled = b;

            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(isDisabled);
                Assert.IsTrue(isDisabled.Value);
            });
        }

        [Test]
        public void TestDisabledWhenSetToTrueNotifiesSubscribers()
        {
            bool? isDisabledA = null;
            bool? isDisabledB = null;
            bool? isDisabledC = null;
            bindableStringList.DisabledChanged += b => isDisabledA = b;
            bindableStringList.DisabledChanged += b => isDisabledB = b;
            bindableStringList.DisabledChanged += b => isDisabledC = b;

            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(isDisabledA);
                Assert.IsTrue(isDisabledA.Value);
                Assert.IsNotNull(isDisabledB);
                Assert.IsTrue(isDisabledB.Value);
                Assert.IsNotNull(isDisabledC);
                Assert.IsTrue(isDisabledC.Value);
            });
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscriber()
        {
            bindableStringList.DisabledChanged += b => Assert.Fail();

            bindableStringList.Disabled = bindableStringList.Disabled;
        }

        [Test]
        public void TestDisabledWhenSetToCurrentValueDoesNotNotifySubscribers()
        {
            bindableStringList.DisabledChanged += b => Assert.Fail();
            bindableStringList.DisabledChanged += b => Assert.Fail();
            bindableStringList.DisabledChanged += b => Assert.Fail();

            bindableStringList.Disabled = bindableStringList.Disabled;
        }

        [Test]
        public void TestDisabledNotifiesBoundLists()
        {
            var list = new BindableList<string>();
            list.BindTo(bindableStringList);

            bindableStringList.Disabled = true;

            Assert.IsTrue(list.Disabled);
        }

        #endregion

        #region .GetEnumberator()

        [Test]
        public void TestGetEnumeratorDoesNotReturnNull()
        {
            Assert.NotNull(bindableStringList.GetEnumerator());
        }

        [Test]
        public void TestGetEnumeratorWhenCopyConstructorIsUsedDoesNotReturnTheEnumeratorOfTheInputtedEnumerator()
        {
            string[] array = { "" };
            var list = new BindableList<string>(array);

            var enumerator = list.GetEnumerator();

            Assert.AreNotEqual(array.GetEnumerator(), enumerator);
        }

        #endregion

        #region .Description

        [Test]
        public void TestDescriptionWhenSetReturnsSetValue()
        {
            const string description = "The list used for testing.";

            bindableStringList.Description = description;

            Assert.AreEqual(description, bindableStringList.Description);
        }

        #endregion

        #region .Parse(obj)

        [Test]
        public void TestParseWithNullClearsList()
        {
            bindableStringList.Add("a item");

            bindableStringList.Parse(null);

            Assert.IsEmpty(bindableStringList);
        }

        [Test]
        public void TestParseWithArray()
        {
            IEnumerable<string> strings = new[] { "testA", "testB" };

            bindableStringList.Parse(strings);

            CollectionAssert.AreEquivalent(strings, bindableStringList);
        }

        [Test]
        public void TestParseWithDisabledListThrowsInvalidOperationException()
        {
            bindableStringList.Disabled = true;

            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Parse(null));
                Assert.Throws(typeof(InvalidOperationException), () => bindableStringList.Parse(new object[]
                {
                    "test", "testabc", "asdasdasdasd"
                }));
            });
        }

        [Test]
        public void TestParseWithInvalidArgumentTypesThrowsArgumentException()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(""));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(new object()));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1.1));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(1.1f));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse("test123"));
                Assert.Throws(typeof(ArgumentException), () => bindableStringList.Parse(29387L));
            });
        }

        [Test]
        public void TestParseWithNullNotifiesClearSubscribers()
        {
            string[] strings = { "testA", "testB", "testC" };
            bindableStringList.AddRange(strings);

#pragma warning disable 618 can be removed 20200817
            bool itemsGotCleared = false;
            IEnumerable<string> clearedItems = null;
            bindableStringList.ItemsRemoved += items =>
            {
                itemsGotCleared = true;
                clearedItems = items;
            };
#pragma warning restore 618
            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringList.Parse(null);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, clearedItems);
                Assert.IsTrue(itemsGotCleared);
            });

            Assert.That(triggeredArgs, Has.Count.EqualTo(1));
            Assert.That(triggeredArgs.First().Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.First().OldItems, Is.EquivalentTo(strings));
            Assert.That(triggeredArgs.First().OldStartingIndex, Is.EqualTo(0));
        }

        [Test]
        public void TestParseWithItemsNotifiesAddRangeAndClearSubscribers()
        {
            bindableStringList.Add("test123");
            IEnumerable<string> strings = new[] { "testA", "testB" };

#pragma warning disable 618 can be removed 20200817
            IEnumerable<string> addedItems = null;
            bool? itemsWereFirstCleaned = null;
            bindableStringList.ItemsAdded += items =>
            {
                addedItems = items;
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = false;
            };
            bindableStringList.ItemsRemoved += items =>
            {
                if (itemsWereFirstCleaned == null)
                    itemsWereFirstCleaned = true;
            };
#pragma warning restore 618
            var triggeredArgs = new List<NotifyCollectionChangedEventArgs>();
            bindableStringList.CollectionChanged += (_, args) => triggeredArgs.Add(args);

            bindableStringList.Parse(strings);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(strings, bindableStringList);
                CollectionAssert.AreEquivalent(strings, addedItems);
                Assert.NotNull(itemsWereFirstCleaned);
                Assert.IsTrue(itemsWereFirstCleaned ?? false);
            });

            Assert.That(triggeredArgs, Has.Count.EqualTo(2));
            Assert.That(triggeredArgs.First().Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(triggeredArgs.First().OldItems, Is.EquivalentTo("test123".Yield()));
            Assert.That(triggeredArgs.First().OldStartingIndex, Is.EqualTo(0));
            Assert.That(triggeredArgs.ElementAt(1).Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.ElementAt(1).NewItems, Is.EquivalentTo(strings));
            Assert.That(triggeredArgs.ElementAt(1).NewStartingIndex, Is.EqualTo(0));
        }

        #endregion

        #region GetBoundCopy()

        [Test]
        public void TestBoundCopyWithAdd()
        {
            var boundCopy = bindableStringList.GetBoundCopy();

#pragma warning disable 618 can be removed 20200817
            bool boundCopyItemAdded = false;
            boundCopy.ItemsAdded += item => boundCopyItemAdded = true;
#pragma warning restore 618
            NotifyCollectionChangedEventArgs triggeredArgs = null;
            boundCopy.CollectionChanged += (_, args) => triggeredArgs = args;

            bindableStringList.Add("test");

            Assert.IsTrue(boundCopyItemAdded);

            Assert.That(triggeredArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(triggeredArgs.NewItems, Is.EquivalentTo("test".Yield()));
            Assert.That(triggeredArgs.NewStartingIndex, Is.EqualTo(0));
        }

        #endregion
    }
}
