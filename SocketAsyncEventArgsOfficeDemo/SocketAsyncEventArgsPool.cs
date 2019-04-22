using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncEventArgsOfficeDemo
{

    //用于存放SAEA对象，应该是为了可重用

    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        //放入SAEA对象
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_pool)       //lock将包含的语句块标记为临界区，一次只能一个线程进入
            {
                m_pool.Push(item);
            }
        }

        //从池中删除SocketAsyncEventArgs实例并返回从池中删除的对象
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        //获取池中SAEA的数量
        public int Count
        {
            get { return m_pool.Count; }
        }

        public void Clear()
        {
            m_pool.Clear();
        }
    }
}
