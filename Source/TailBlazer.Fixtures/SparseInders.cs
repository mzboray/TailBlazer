﻿using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using TailBlazer.Domain.FileHandling;
using Xunit;

namespace TailBlazer.Fixtures
{
    public class SparseIndexerFixture
    {

        [Fact]
        public  void CanProduceIndices()
        {
            var file = Path.GetTempFileName();
            var info = new FileInfo(file);
            File.AppendAllLines(file, Enumerable.Range(1, 10000).Select(i => $"This is line number {i.ToString("00000000")}").ToArray());

            var refresher = new Subject<Unit>();
            var scheduler = new TestScheduler();

            using (var indexer = new SparseIndexer(info, refresher, tailSize: 1000, scheduler: scheduler))
            {
      
                SparseIndicies result = null;
                using (indexer.Result.Subscribe(indicies => result = indicies))
                {
                    //start off the head scanner
                    scheduler.AdvanceBy(1);
                    var totalCount = result.Count;

                    //there could be an overlapping index (the index collection will deal with this)
                    totalCount.Should().Be(10001);
                }
            }
        
          File.Delete(file);
        }


        [Fact]
        public  void WillAutoTail()
        {
            var file = Path.GetTempFileName();
            var info = new FileInfo(file);
            var scheduler = new TestScheduler();
            File.AppendAllLines(file, Enumerable.Range(1, 10000).Select(i => $"This is line number {i.ToString("00000000")}").ToArray());


            var refresher = new Subject<Unit>();

            using (var indexer = new SparseIndexer(info, refresher,tailSize: 1000, scheduler: scheduler))
            {

                SparseIndicies result = null;

                using (indexer.Result.Subscribe(indicies => result = indicies))
                {
                    scheduler.AdvanceBy(1);


                    //there could be an overlapping index (the index collection will deal with this)
                    result.Count.Should().Be(10001);


                    File.AppendAllLines(file, Enumerable.Range(10000, 10).Select(i => $"This is line number {i.ToString("00000000")}").ToArray());

                    refresher.OnNext(Unit.Default);

                    //force notification
                    //  indexer.Refresh();
                   // refresher.OnNext(Unit.Default);



                    result.Count.Should().Be(10011);
                }
            }

            File.Delete(file);
        }
    }
}
