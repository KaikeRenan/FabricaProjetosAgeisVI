"""Dados MOCK para o MVP: simulador com processo gerador (DGP) conhecido, livro-razão coerente e diagnósticos.

Por que um simulador e não "linhas aleatórias": com um DGP conhecido sabemos a demanda VERDADEIRA e podemos
(1) validar o gerador contra a própria especificação, (2) medir o piso de erro irredutível (oráculo) e
(3) medir o efeito de censura por ruptura. Nada aqui é dado real; ver docs/MOCK_DATA.md.
"""
from .dgp import MockConfig, CATALOG  # noqa: F401
from .simulate import MockDataset, simulate  # noqa: F401
